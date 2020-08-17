using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;

namespace NextLevelLibrary.MarioStrikers
{
    public class ModelChunk
    {
        public uint MeshCount { get; set; }
        public uint HashID { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }

        public ModelChunk(FileReader reader, bool isGamecube)
        {
            if (isGamecube) {
                MeshCount = reader.ReadUInt32();
                HashID = reader.ReadUInt32();
            }
            else {
                HashID = reader.ReadUInt32();
                MeshCount = reader.ReadUInt32();
            }
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
        }

        public void Write(FileWriter writer)
        {

        }
    }
}
