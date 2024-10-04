using System;
using System.Collections.Generic;

namespace CowApplication
{
    public class CowModel
    {
        public Guid Id { get; set; }
        public string Breed { get; set; }
        public int Age { get; set; }
        public string Name { get; set; }
        public byte[] DnaData { get; set; }
    }
}
