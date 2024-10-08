using System;
using System.Collections.Generic;

namespace Core
{
    public interface IRecordStorage
    {
        // Effectively update an recorde
        void Update(uint recordId, byte[] data);

        // Grab a record's data
        byte[] Find(uint recordId);

        // This creates new empty record
        uint Create();

        // This creates new record with given data and returns its ID
        uint Create(byte[] data);

        // Similar to Create(byte[] data), but with dataGenerator which generates data after a record is allocated
        uint Create(Func<uint, byte[]> dataGenerator);

        // This deletes a record by its id
        void Delete(uint recordId);
    }
}
