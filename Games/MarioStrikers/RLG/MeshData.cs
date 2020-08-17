using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;

namespace NextLevelLibrary.MarioStrikers
{
    public class MeshData
    {
        public uint[] BoneHashes;

        public enum PolygonType : byte
        {
            Triangles = 0,
            TriangleStrips = 1,
        }

        public uint IndexStartOffset;
        public ushort IndexFormat;
        public ushort IndexCount;
        public ushort VertexCount;
        public byte Unknown;
        public byte NumAttributePointers;

        public uint HashID;
        public uint MaterialHashID;

        //Only on gamecube (wii uses seperate section)
        public uint TexturHashID;

        public uint MaterialOffset;

        public MaterailPresets MaterailPreset;

        public PolygonType FaceType = PolygonType.TriangleStrips;

        public MeshData(FileReader reader, bool isGamecube)
        {
            if (isGamecube)
            {
                reader.ReadUInt16(); //0
                IndexFormat = reader.ReadUInt16();
                IndexStartOffset = reader.ReadUInt32();
                IndexCount = reader.ReadUInt16();
                FaceType = reader.ReadEnum<PolygonType>(false);
                NumAttributePointers = reader.ReadByte();
                reader.ReadUInt32();
                MaterialHashID = reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                MaterialOffset = reader.ReadUInt32();
                reader.ReadUInt32();
                TexturHashID = reader.ReadUInt32();
                reader.ReadUInt32();
            }
            else
            {
                IndexStartOffset = reader.ReadUInt32();
                IndexFormat = reader.ReadUInt16();
                IndexCount = reader.ReadUInt16();
                VertexCount = reader.ReadUInt16();
                Unknown = reader.ReadByte();
                NumAttributePointers = reader.ReadByte();
                reader.ReadUInt32();
                MaterialHashID = reader.ReadUInt32();
                HashID = reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                MaterialOffset = reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
            }

            MaterailPreset = (MaterailPresets)MaterialHashID;
        }
    }
}
