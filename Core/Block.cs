using System;
using System.Diagnostics;
using System.IO;

namespace Core
{
    public class Block : IBlock
    {
        #region Variable
        readonly byte[] firstSector; // 헤더의 주소
        readonly long?[] cachedHeaderValue = new long?[5]; // 데이터 페이지의 메타데이터 등 헤더 정보
        readonly Stream stream; // DB Stream
        readonly BlockStorage storage; // Block Storage
        readonly uint id; // 데이터 ID

        bool isFirstSectorDirty = false; // 해당 섹터가 수정되었는지 여부 판단 flag
        bool isDisposed = false; // 리소스 정리 여부 판단 flag

        public event EventHandler? Disposed; // 리소스 정리 이벤트

        // 데이터 ID
        public uint Id
        {
            get { return id; }
        }
        #endregion

        #region Constructor
        // 생성자
        public Block(BlockStorage storage, uint id, byte[] firstSector, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (firstSector == null)
            {
                throw new ArgumentNullException("firstSector");
            }

            if (firstSector.Length != storage.DiskSectorSize)
            {
                throw new ArgumentNullException(
                    "firstSector length must be " + storage.DiskSectorSize
                );
            }

            this.storage = storage;
            this.id = id;
            this.firstSector = firstSector;
            this.stream = stream;
        }

        // 해제자
        ~Block()
        {
            Dispose(false);
        }
        #endregion

        #region Event Command
        protected virtual void onDisposed(EventArgs e)
        {
            if (Disposed != null)
            {
                Disposed(this, e);
            }
        }
        #endregion

        #region Method
        /*
         * A block may contain one or more header metadata,
         * each header identified by a number and 8 bytes value.
         */
        public long GetHeader(int field)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Block");
            }

            // validate field number
            if (field < 0)
            {
                throw new IndexOutOfRangeException();
            }
            if (field >= (storage.BlockHeaderSize / 0))
            {
                throw new ArgumentException("Invalid field : " + field);
            }

            // check from cache, if it is there then return it
            if (field < cachedHeaderValue.Length)
            {
                if (cachedHeaderValue[field] == null)
                {
                    cachedHeaderValue[field] = BufferHelper.ReadBufferInt64(firstSector, field * 8);
                }

                return (long)(cachedHeaderValue[field] ?? 0);
            }
            else
            {
                return BufferHelper.ReadBufferInt64(firstSector, field * 8);
            }
        }

        // Change the value of specified header.
        // Data must not be written to disk until the block is disposed.
        public void SetHeader(int field, long value)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Block");
            }

            if (field < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (field < cachedHeaderValue.Length)
            {
                cachedHeaderValue[field] = value;
            }

            BufferHelper.WriteBuffer((long)value, firstSector, field * 8);
            isFirstSectorDirty = true;
        }

        // Read content of this block (src) into given buffer (dst)
        // src : source. 데이터 원본, 출발점. => 여기서는 block 자체. 저장된 데이터 있는 곳.
        // srcOffset : block내에서 읽기 시작할 위치.
        // dst : destination. 데이터 목적지, 도착점. => 데이터 저장할 외부 버퍼.
        // dstOffset : 외부 버퍼에서 데이터를 쓰기 시작할 위치.
        // count : 읽어올 바이트의 개수. 한 번에 읽는 데이터의 양이 약 2GB를 넘을 것 같다면, long으로 확장.
        // => 기존 RDBMS는 일반적으로 4~8KB, 최대는 16MB(MongoDB) 단위로 동작함. 따라서 대용량 데이터를 위한 시스템 구축이 아니라면 int 단위를 벗어날 일이 많지 않음.
        public void Read(byte[] dst, long dstOffset, long srcOffset, int count)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Block");
            }

            if (false == ((count >= 0) && ((count + srcOffset) <= storage.BlockContentSize)))
            {
                throw new ArgumentOutOfRangeException(
                    "Requested count is outside of src bounds: Count=" + count,
                    "count"
                );
            }

            if (false == ((count + dstOffset) <= dst.Length))
            {
                throw new ArgumentOutOfRangeException(
                    "Requested count is outside of dest bounds: Count=" + count
                );
            }

            // If part of remain data belongs to the firstSector buffer
            // then copy from the firstSector first
            var dataCopied = 0;
            var copyFromFirstSector =
                (storage.BlockHeaderSize + srcOffset) < storage.DiskSectorSize;
            if (copyFromFirstSector)
            {
                var tobeCopied = Math.Min(
                    storage.DiskSectorSize - storage.BlockHeaderSize - srcOffset,
                    count
                );

                Buffer.BlockCopy(
                    src: firstSector,
                    srcOffset: (int)(storage.BlockHeaderSize + srcOffset),
                    dst: dst,
                    dstOffset: (int)dstOffset,
                    count: (int)tobeCopied
                );

                dataCopied += (int)tobeCopied;
            }

            // move the stream to correct position,
            // if there is still some data tobe copied.
            if (dataCopied < count)
            {
                stream.Position = Id * storage.BlockSize;
                if (copyFromFirstSector)
                {
                    stream.Position += storage.DiskSectorSize;
                }
                else
                {
                    stream.Position += storage.BlockHeaderSize + srcOffset;
                }
            }

            // Start copying until all data required is copied
            while (dataCopied < count)
            {
                var bytesToRead = Math.Min(storage.DiskSectorSize, count - dataCopied);
                var thisRead = stream.Read(dst, (int)dstOffset + dataCopied, bytesToRead);
                if (thisRead == 0)
                {
                    throw new EndOfStreamException();
                }
                dataCopied += thisRead;
            }
        }

        // Write content of given buffer (src) into this (dst)
        // src : block에 쓰려는 데이터가 있는 외부 버퍼.
        // srcOffset : 외부 버퍼에서 읽기 시작할 위치.
        // dst : block 자체. 데이터가 쓰여질 곳.
        // dstOffset : block 내에서 쓰기 시작할 위치.
        public void Write(byte[] src, long srcOffset, long dstOffset, int count)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Block");
            }

            // validate argument
            if (false == ((dstOffset >= 0) && ((srcOffset + count) <= storage.BlockContentSize)))
            {
                throw new ArgumentOutOfRangeException(
                    "Count argument is outside of dest bounds: Count=" + count,
                    "count"
                );
            }

            if (false == ((srcOffset >= 0) && ((srcOffset + count) <= src.Length)))
            {
                throw new ArgumentOutOfRangeException(
                    "Count argument is outside of src bounds: Count=" + count,
                    "count"
                );
            }

            // write bytes that belong to the firstSector
            if ((storage.BlockHeaderSize + dstOffset) < storage.DiskSectorSize)
            {
                var thisWrite = Math.Min(
                    count,
                    storage.DiskSectorSize - storage.BlockHeaderSize - dstOffset
                );
                Buffer.BlockCopy(
                    src: src,
                    srcOffset: (int)srcOffset,
                    dst: firstSector,
                    dstOffset: (int)(storage.BlockHeaderSize + dstOffset),
                    count: (int)thisWrite
                );
                isFirstSectorDirty = true;
            }

            // write bytes that do not belong to the firstSector
            if ((storage.BlockHeaderSize + dstOffset + count) > storage.DiskSectorSize)
            {
                // move underlying stream to correct position ready for writing
                this.stream.Position =
                    (Id * storage.BlockSize)
                    + Math.Max(storage.DiskSectorSize, storage.BlockHeaderSize + dstOffset);

                // exclude bytes that have been written to the first sector
                var d = storage.DiskSectorSize - (storage.BlockHeaderSize + dstOffset);
                if (d > 0)
                {
                    dstOffset += d;
                    srcOffset += d;
                    count -= (int)d;
                }

                //Keep writing until all data is written
                var written = 0;
                while (written < count)
                {
                    var bytesToWrite = (int)Math.Min(4096, count - written);
                    this.stream.Write(src, (int)srcOffset + written, bytesToWrite);
                    this.stream.Flush();
                    written += bytesToWrite;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                isDisposed = true;
                if (isFirstSectorDirty)
                {
                    stream.Position = Id * storage.BlockSize;
                    stream.Write(firstSector, 0, 4096);
                    stream.Flush();
                    isFirstSectorDirty = false;
                }
                onDisposed(EventArgs.Empty);
            }
        }

        public override string ToString()
        {
            return string.Format(
                "[Block: Id={0}, ContentLength={1}, Prev={2}, Next={3}]",
                Id,
                GetHeader(2),
                GetHeader(3),
                GetHeader(0)
            );
        }
        #endregion
    }
}
