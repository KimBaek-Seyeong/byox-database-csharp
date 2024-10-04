using System;
using System.Diagnostics;
using System.IO;

namespace Core
{
    public class Block : IBlock
    {
        #region Variables
        readonly byte[] firstSector; // 헤더의 주소
        readonly long?[] cachedHeaderValue = new long?[5]; // 데이터 페이지의 메타데이터 등 헤더 정보
        readonly Stream stream; // DB Stream
        readonly BlockStorage storage; // Block Storage
        readonly uint id; // 데이터 ID

        bool isFirstSectorDirty = false; // 해당 섹터가 수정되었는지 여부 판단 flag
        bool isDisposed = false; // 리소스 정리 여부 판단 flag

        public event EventHandler Disposed; // 리소스 정리 이벤트

        // 데이터 ID
        public uint Id
        {
            get { return id; }
        }
        #endregion

        #region Constructors
        // 생성자
        public Block(BlockStorage storage, uint id, byte[] firstSector, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
        }

        // 해제자
        ~Block()
        {
            Dispose(false);
        }
        #endregion

        #region Event Command
        onDisposed();
        #endregion

        #region Method
        // A block may contain one or more header metadata,
        // each header identified by a number and 8 bytes value.
        public long GetHeader(int field);

        // Change the value of specified header.
        // Data must not be written to disk until the block is disposed.
        public void SetHeader(int field, long value);

        // Read content of this block (src) into given buffer (dst)
        // src : source. 데이터 원본, 출발점. => 여기서는 block 자체. 저장된 데이터 있는 곳.
        // srcOffset : block내에서 읽기 시작할 위치.
        // dst : destination. 데이터 목적지, 도착점. => 데이터 저장할 외부 버퍼.
        // dstOffset : 외부 버퍼에서 데이터를 쓰기 시작할 위치.
        // count : 읽어올 바이트의 개수. 한 번에 읽는 데이터의 양이 약 2GB를 넘을 것 같다면, long으로 확장. => 기존 RDBMS는 일반적으로 4~8KB, 최대는 16MB(MongoDB) 단위로 동작함. 따라서 대용량 데이터를 위한 시스템 구축이 아니라면 int 단위를 벗어날 일이 많지 않음.
        public void Read(byte[] dst, long dstOffset, long srcOffset, int count);

        // Write content of given buffer (src) into this (dst)
        // src : block에 쓰려는 데이터가 있는 외부 버퍼.
        // srcOffset : 외부 버퍼에서 읽기 시작할 위치.
        // dst : block 자체. 데이터가 쓰여질 곳.
        // dstOffset : block 내에서 쓰기 시작할 위치.
        public void Write(byte[] src, long srcOffset, long dstOffset, int count);

        private void Dispose();
        #endregion
    }
}
