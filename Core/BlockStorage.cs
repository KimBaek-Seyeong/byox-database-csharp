using System;

namespace Core
{
    public class BlockStorage : IBlockStorage
    {
        #region Variable
        readonly uint blockId; //
        readonly int blockContentSize; //
        readonly int blockHeaderSize; //
        readonly int blockSize; //
        readonly int unitOfWork; //
        readonly Stream stream; //
        readonly Dictionary<uint, Block> blocks = new Dictionary<uint, Block>();

        //
        public int BlockContentSize
        {
            get { return blockContentSize; }
        }

        //
        public int BlockHeaderSize
        {
            get { return blockHeaderSize; }
        }

        //
        public int BlockSize
        {
            get { return blockSize; }
        }

        //
        public int DiskSectorSize
        {
            get { return unitOfWork; }
        }
        #endregion

        #region Constructor
        /*
         * 128 bytes are determined to be a randomly specified minimum value.
         * Therefore, if necessary, check the overall database performance and usage patterns,
         * such as I/O efficiency and memory usage, and change those value.
         */
        public BlockStorage(Stream storage, int blockSize = 40960, int blockHeaderSize = 48)
        {
            if (storage == null)
            {
                throw new ArgumentNullException("storage");
            }

            if (blockHeaderSize >= blockSize)
            {
                throw new ArgumentException(
                    "blockHeaderSize cannot be " + "larger than or equal " + "to " + "blockSize"
                );
            }

            if (blockSize < 128)
            {
                throw new ArgumentException("blockSize too small");
            }

            this.blockSize = blockSize;
            this.blockHeaderSize = blockHeaderSize;
            blockContentSize = blockSize - blockHeaderSize;
            unitOfWork = (blockSize >= 4096) ? 4096 : 128;
            stream = storage;
        }
        #endregion

        #region Method
        //Find a block by its id
        public IBlock? Find(uint blockId)
        {
            if (blocks.TryGetValue(blockId, out Block? value))
            {
                return value;
            }

            // First, move to that block.
            // If there is no such block return NULL
            var blockPosition = blockId * blockSize;
            if ((blockPosition + blockSize) > stream.Length)
            {
                return null;
            }

            // Read the first 4KB of the block to construct a block from it
            var firstSector = new byte[DiskSectorSize];
            stream.Position = blockId * blockSize;
            stream.Read(firstSector, 0, DiskSectorSize);

            var block = new Block(this, blockId, firstSector, this.stream);
            OnBlockInitialized(block);
            return block;
        }

        // Allocate new block, extend the legth of underlying storage
        public IBlock CreateNew()
        {
            if (this.stream.Length % blockSize != 0)
            {
                throw new DataMisalignedException(
                    "Unexpected length of the stream: " + this.stream.Length
                );
            }

            // calculate new block Id
            var blockId = (uint)Math.Ceiling((double)this.stream.Length / (double)blockSize);

            // extend length of underlying stream
            this.stream.SetLength((long)((blockId * blockSize) + blockSize));
            this.stream.Flush();
            /*
             * In Java, Flush() sends the contents of the buffer to the output stream and empties the buffer.
             * Using Flush() in C# sends the contents of the buffer to the underlying stream,
             * but there is no explicit mention of emptying the buffer.
             * Therefore, it is recommended to use Close() or Dispose() instead of Flush()
             * when dealing with "stream" in C#, or to use a "using" statement.
             * Nevertheless, if you want to use Flush(), consider using with Clear() or
             * using FlushAsync(): an asynchronous version of Flush().
             *
             * The main reason for using the "using" statement in C# has to do with resource management.
             * The Garbage Collector(GC) primarily handles managed memory and
             * explicitly releases non-managed resources, such as file handles, through using.
             * Java uses the "try-with-resources" statement to provide similar functionality,
             * and prior to Java 7, the "try-finally" block was mainly used.
             *
             * The advantage of using the "using" statement is that when the "using" block ends,
             * the Dispose() method is automatically invoked to ensure that
             * the resource is undone even if an exception occurs,
             * and the developer can prevent the mistake of forgetting the Dispose() message.
             */

            // return desired block
            var block = new Block(this, blockId, new byte[DiskSectorSize], this.stream);
            OnBlockInitialized(block);
            return block;
        }

        //
        protected virtual void OnBlockInitialized(Block block)
        {
            // Keep preference to it
            blocks[block.Id] = block;
            // when block disposed, remove it from memory
            block.Disposed += HandleBlockDisposed;
        }

        /*
        ISSUE :
        The 'sender' parameter of 'void BlockStorage.HandleBlockDisposed(object sender, EventArgs e)' does not match
        the target agent 'EventHandler' to allow null of the reference format.

        CAUSE :
        The warning is related to "Nullable Reference Types": a feature introduced in C# 8.0 or higher versions.
        The "sender" parameter of "EventHandler" delegate doesn't allow null,
        whereas in your method, it is treated as if it could allow null.

        SOLUTION :
        Clarify the intention of the code and prevent potential null reference errors.

        //
        protected virtual void HandleBlockDisposed(object sender, EventArgs e)
        {
            // Stop listening to it
            var block = (Block)sender;
            block.Disposed -= HandleBlockDisposed;

            // Remove it from memory
            blocks.Remove(block.Id);
        }
        */
        //
        protected virtual void HandleBlockDisposed(object? sender, EventArgs e)
        {
            if (sender is Block block)
            {
                // Stop listening to it
                block.Disposed -= HandleBlockDisposed;
                // Remove it from memory
                blocks.Remove(block.Id);
            }
        }
        #endregion
    }
}
