using System;

namespace Core
{
    public interface IBlockStorage
    {
        // Number of bytes of custom data per block that this storage can handle.
        int BlockContentSize { get; }

        // Total number of bytes in header
        int BlockHeaderSize { get; }

        // Total block size, equal to content size + header size, should be a multiple of 128B
        int BlockSize { get; }

        //Find a block by its id
        IBlock? Find(uint blockId);

        //Allocate new block, extend the legth of underlying storage
        IBlock CreateNew();
    }
}
