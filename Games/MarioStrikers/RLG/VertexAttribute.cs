using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;

namespace NextLevelLibrary.MarioStrikers
{
    public class VertexAttribute
    {
        public uint Offset;
        public byte Stride;
        public byte Type;
        public byte Format;

        public VertexAttribute(FileReader reader, bool isGamecube)
        {
            if (isGamecube)
            {
                Offset = reader.ReadUInt32();
                Type = reader.ReadByte();
                Stride = reader.ReadByte();
            }
            else
            {
                Offset = reader.ReadUInt32();
                Type = reader.ReadByte();
                Stride = reader.ReadByte();
                reader.ReadUInt16();
            }
        }

        public void Write(FileWriter writer)
        {

        }
    }
}
