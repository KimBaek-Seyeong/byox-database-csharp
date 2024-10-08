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

            this.unitOfWork = ((blockSize >= 4096) ? 4096 : 128);
            this.blockSize = blockSize;
            this.blockHeaderSize = blockHeaderSize;
            this.blockContentSize = blockSize - blockHeaderSize;
        }
        #endregion

        #region Method
        //Find a block by its id
        public Block Find(uint blockId) { }

        //Allocate new block, extend the legth of underlying storage
        public Block CreateNew();
        #endregion
    }
}
