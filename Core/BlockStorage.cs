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
        public BlockStorage() { }
        #endregion

        #region Method
        //Find a block by its id
        public Block Find(uint blockId) { }

        //Allocate new block, extend the legth of underlying storage
        public Block CreateNew();
        #endregion
    }
}
