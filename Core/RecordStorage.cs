using System;
using System.IO;

namespace Core
{
    public class RecordStorage : IRecordStorage
    {
        #region Variable
        readonly IBlockStorage storage;

        const int MaxRecordSize = 4194304; // 4MB
        cosnt int kNextBlockId = 0;
        const int kRecordLength = 1;
        const int kBlockContentLength = 2;
        const int kPreviousBlockId = 3;
        const int kIsDeleted = 4;
        /*
        * The common interpretation of the prefix 'k' is 'konstant' (constant number).
        * In the early days of C++ development, a naming convention called Hungarian notation was widely used.
        * At that time, people began using the prefix 'k' to denote constant numbers.
        * The exact reason for choosing 'k' instead of 'c' (for 'constant') is not known,
        * but most developers belive it comes from the German word 'konstant', which means 'constant'.
        */
        #endregion

        #region Constructor
        public RecordStorage(IBlockStorage storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException("storage");
            }

            this.storage = storage;

            if (storage.BlockHeaderSize < 48)
            {
                throw new ArgumentException("Record storage needs at least 48 header bytes");
            }
        }
        #endregion

        #region Event Command
        #endregion

        #region Method
        // Effectively update an recorde
        public void Update(uint recordId, byte[] data) { }

        // Grab a record's data
        public virtual byte[] Find(uint recordId)
        {
            // first grab the block
            using (var block = storage.Find(recordId))
            {
                if (block == null)
                {
                    return null;
                }
L
                // if this is a deleted block then ignore it
                if (1L == block.GetHeader(kIsDeleted))
                { 
                    return null;
                }

                // if this block is a child block then also ignore it
                if (0L != block.GetHeader (kPreviousBlockId))
                {
                    return null;
                }

                // grab total record size and allocate corresponded memory
                var totalRecordSize = block.GetHeader(kRecordLength);
                if (totalRecordSize > MaxRecordSize) {
                    throw new NotSupportedException ("Unexpected record length: " + totalRecordSize);
                }

                var data = new byte[totalRecordSize];
                var bytesRead = 0;

                // now start filling data
                IBlock currentBlock = block;
                while(true) {
                    uint nextBlockId;

                    using (currentBlock) {
                        var thisBlockContentLength = currentBlock.GetHeader(kBlockContentLength);
                        if (thisBlockContentLength > storage.BlockContentSize) {
                            throw new InvalidDataException("Unexpected block content length: " + thisBlockContentLength);
                        }

                        // read all available content of current block
                        currentBlock.Read (dst: data, dstOffset: bytesRead, srcOffset:0, count:(int)thisBlockContentLength);

                        // update number of bytes read
                        bytesRead += (int)thisBlockContentLength;

                        // move to the next block if there is any
                        nextBlockId = (uint)currentBlock.GetHeader (kNextBlockId);
                        if (nextBlockId == 0) {
                            return data;
                        }
                    }

                    currentBlock = storage.Find(nextBlockId);
                    if (currentBlock == null) {
                        throw new InvalidDataException ("Block not found by id : "+ nextBlockId);
                    }
                }
            }
        }

        // This creates new empty record
        public uint Create() { }

        // This creates new record with given data and returns its ID
        public uint Create(byte[] data) { }

        // Similar to Create(byte[] data), but with dataGenerator which generates data after a record is allocated
        public uint Create(Func<uint, byte[]> dataGenerator) { }

        // This deletes a record by its id
        public void Delete(uint recordId) { }
        #endregion
    }
}
