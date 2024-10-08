using System;
using System.IO;

namespace Core
{
    public class RecordStorage : IRecordStorage
    {
        #region Variable
        #endregion

        #region Constructor
        public RecordStorage() { }
        #endregion

        #region Event Command
        #endregion

        #region Method
        // Effectively update an recorde
        public void Update(uint recordId, byte[] data) { }

        // Grab a record's data
        public byte[] Find(uint recordId) { }

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
