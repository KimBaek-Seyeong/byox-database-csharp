using System;
using System.IO;

namespace Core
{
    public class RecordStorage : IRecordStorage
    {
        #region Variable
        readonly IBlockStorage storage;

        const int MaxRecordSize = 4194304; // 4MB
        const int kNextBlockId = 0;
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

        #region Method
        // Effectively update an recorde
        public void Update(uint recordId, byte[] data)
        {
            var written = 0;
            var total = data.Length;
            var blocks = FindBlocks(recordId);
            var blockUsed = 0;
            var previousBlock = (IBlock)null;

            try
            {
                // start writing blck by block ..
                while (written < total)
                {
                    // bytes to be written in this block
                    var bytesToWrite = Math.Min(total - written, storage.BlockContentSize);

                    // get the blcok where the first byte of remainng data will be written to
                    var blockIndex = (int)
                        Math.Floor((double)written / (double)storage.BlockContentSize);

                    // find the block to write to:
                    // if 'blockIndex' exists in 'blocks', then write into it,
                    // otherwise allocate a new one for writting
                    var target = (IBlock)null;
                    if (blockIndex < blocks.Count)
                    {
                        target = blocks[blockIndex];
                    }
                    else
                    {
                        target = AllocateBlock();
                        if (target == null)
                        {
                            throw new Exception("Failed to allocate new block");
                        }
                        blocks.Add(target);
                    }

                    // link with previous block
                    if (previousBlock != null)
                    {
                        previousBlock.SetHeader(kNextBlockId, target.Id);
                        target.SetHeader(kPreviousBlockId, previousBlock.Id);
                    }

                    // write data
                    target.Write(src: data, srcOffset: written, dstOffset: 0, count: bytesToWrite);
                    target.SetHeader(kBlockContentLength, bytesToWrite);
                    target.SetHeader(kNextBlockId, 0);
                    if (written == 0)
                    {
                        target.SetHeader(kRecordLength, total);
                    }

                    // get ready fr next loop
                    blockUsed++;
                    written += bytesToWrite;
                    previousBlock = target;
                }

                // after writing, delete off any unused blocks
                if (blocksUsed < blocks.Count)
                {
                    for (var i = blockUsed; i < blocks.Count; i++)
                    {
                        MarkAsFree(blocks[i].Id);
                    }
                }
            }
            finally
            {
                // always dispose all fetched blocks after finish using them
                foreach (var block in blocks)
                {
                    block.Dispose();
                }
            }
        }

        // Grab a record's data
        public virtual byte[]? Find(uint recordId)
        {
            // first grab the block
            using (var block = storage.Find(recordId))
            {
                if (block == null)
                {
                    return null;
                }

                // if this is a deleted block then ignore it
                if (1L == block.GetHeader(kIsDeleted))
                {
                    return null;
                }

                // if this block is a child block then also ignore it
                if (0L != block.GetHeader(kPreviousBlockId))
                {
                    return null;
                }

                // grab total record size and allocate corresponded memory
                var totalRecordSize = block.GetHeader(kRecordLength);
                if (totalRecordSize > MaxRecordSize)
                {
                    throw new NotSupportedException("Unexpected record length: " + totalRecordSize);
                }

                var data = new byte[totalRecordSize];
                var bytesRead = 0;

                // now start filling data
                IBlock? currentBlock = block;
                while (true)
                {
                    uint nextBlockId;

                    using (currentBlock)
                    {
                        var thisBlockContentLength = currentBlock.GetHeader(kBlockContentLength);
                        if (thisBlockContentLength > storage.BlockContentSize)
                        {
                            throw new InvalidDataException(
                                "Unexpected block content length: " + thisBlockContentLength
                            );
                        }

                        // read all available content of current block
                        currentBlock.Read(
                            dst: data,
                            dstOffset: bytesRead,
                            srcOffset: 0,
                            count: (int)thisBlockContentLength
                        );

                        // update number of bytes read
                        bytesRead += (int)thisBlockContentLength;

                        // move to the next block if there is any
                        nextBlockId = (uint)currentBlock.GetHeader(kNextBlockId);
                        if (nextBlockId == 0)
                        {
                            return data;
                        }
                    }

                    currentBlock = storage.Find(nextBlockId);
                    if (currentBlock == null)
                    {
                        throw new InvalidDataException("Block not found by id : " + nextBlockId);
                    }
                }
            }
        }

        // This creates new empty record
        public virtual uint Create()
        {
            using (var firstBlock = AllocateBlock())
            {
                return firstBlock.Id;
            }
        }

        // This creates new record with given data and returns its ID
        public virtual uint Create(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentException();
            }

            return Create(recordId => data);
        }

        // Similar to Create(byte[] data), but with dataGenerator whih generates data after a record is allocated
        public virtual uint Create(Func<uint, byte[]> dataGenerator)
        {
            if (dataGenerator == null)
            {
                throw new ArgumentException();
            }

            using (var firstBlock = AllocateBlock())
            {
                var returnId = firstBlock.Id;

                // alright now begin writing data
                var data = dataGenerator(returnId);
                var dataWritten = 0;
                var dataTobeWritten = data.Length;
                firstBlock.SetHeader(kRecordLength, dataTobeWritten);

                // if no data tobe written,
                // return this block straight away
                if (dataTobeWritten == 0)
                {
                    return returnId;
                }

                // otherwise continue to write data until completion
                IBlock currentBlock = firstBlock;
                while (dataWritten < dataTobeWritten)
                {
                    IBlock? nextBlock = null;
                    using (currentBlock)
                    {
                        // write as much as possible to this block
                        var thisWrite = (int)
                            Math.Min(storage.BlockContentSize, dataTobeWritten - dataWritten);
                        currentBlock.Write(data, dataWritten, 0, thisWrite);
                        currentBlock.SetHeader(kBlockContentLength, (long)thisWrite);
                        dataWritten += thisWrite;

                        // if still there are data tobe written,
                        // move to the next block
                        if (dataWritten < dataTobeWritten)
                        {
                            nextBlock = AllocateBlock();
                            var success = false;
                            try
                            {
                                nextBlock.SetHeader(kPreviousBlockId, currentBlock.Id);
                                currentBlock.SetHeader(kNextBlockId, nextBlock.Id);
                                success = true;
                            }
                            finally
                            {
                                if ((false == success) && (nextBlock != null))
                                {
                                    nextBlock.Dispose();
                                    nextBlock = null;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    // move to the next block if possible
                    if (nextBlock != null)
                    {
                        currentBlock = nextBlock;
                    }
                }

                // return id of the first block that got dequeued
                return returnId;
            }
        }

        // This deletes a record by its id
        public virtual void Delete(uint recordId)
        {
            using (var block = storage.Find(recordId))
            {
                IBlock? currentBlock = block;
                while (true)
                {
                    IBlock? nextBlock = null;

                    using (currentBlock)
                    {
                        MarkAsFree(currentBlock.Id);
                        currentBlock.SetHeader(kIsDeleted, 1L);

                        var nextBlockId = (uint)currentBlock.GetHeader(kNextBlockId);
                        if (nextBlockId == 0)
                        {
                            break;
                        }
                        else
                        {
                            nextBlock = storage.Find(nextBlockId);
                            if (currentBlock == null)
                            {
                                throw new InvalidDataException(
                                    "Block not found by id: " + nextBlockId
                                );
                            }
                        }
                    }

                    // move to next block
                    if (nextBlock != null)
                    {
                        currentBlock = nextBlock;
                    }
                }
            }
        }

        // Find all blocks of given record, return these blocks in order.
        private List<IBlock> FindBlocks(uint recordId)
        {
            var blocks = new List<IBlock>();
            var success = false;

            try
            {
                var currentBlockId = recordId;

                do
                {
                    // grab next block
                    var block = storage.Find(currentBlockId);
                    if (null == block)
                    {
                        // special case: if block #0 never created, then attempt to create it
                    }
                } while (currentBlockId != 0);

                success = true;
                return blocks;
            }
            finally
            {
                // Incase shit happens, dispose all fetched blocks
                if (false == success)
                {
                    foreach (var block in blocks)
                    {
                        block.Dispose();
                    }
                }
            }
        }

        #endregion
    }
}
