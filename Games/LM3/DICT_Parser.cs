using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace NextLevelLibrary.LM3
{
    public class DICT_Parser : IDictionaryData
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ChunkInfo
        {
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public uint Unknown4;
            public uint Unknown5;
            public uint Unknown6;
        }

        public IEnumerable<Block> BlockList => Blocks;
        public bool BlocksCompressed => IsCompressed;

        public List<ChunkInfo> ChunkInfos = new List<ChunkInfo>();
        public List<Block> Blocks = new List<Block>();
        public List<string> StringList = new List<string>();

        public ushort HeaderFlags = 0x4001;

        public bool IsCompressed = false;

        public string FilePath { get; set; }

        public DICT_Parser(Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                uint Identifier = reader.ReadUInt32();
                HeaderFlags = reader.ReadUInt16();
                IsCompressed = reader.ReadByte() == 1;
                reader.ReadByte(); //Padding
                uint SizeLargestFile = reader.ReadUInt32();
                byte numFiles = reader.ReadByte();
                byte numChunkInfos = reader.ReadByte();
                byte numStrings = reader.ReadByte();
                reader.ReadByte(); //padding
                ChunkInfos = reader.ReadMultipleStructs<ChunkInfo>(numChunkInfos);
                for (int i = 0; i < numFiles; i++)
                {
                    Blocks.Add(new Block(i)
                    {
                        Offset = reader.ReadUInt32(),
                        DecompressedSize = reader.ReadUInt32(),
                        CompressedSize = reader.ReadUInt32(),
                        Flags = reader.ReadUInt32(),
                    });

                    //Handle the flags
                    uint resourceFlag = Blocks[i].Flags & 0xFF;
                    uint resourceFlag2 = Blocks[i].Flags >> 24 & 0xFF;
                    uint resourceIndex = Blocks[i].Flags >> 16 & 0xFF;

                    //This combimation determines a file table
                    if (resourceFlag == 0x03 && resourceFlag2 == 1)
                        Blocks[i].SourceType = ResourceType.TABLE;
                    else if (resourceFlag != 0)
                        Blocks[i].SourceType = ResourceType.DATA;

                    //The source index determines which external file to use
                    Blocks[i].SourceIndex = (byte)resourceIndex;
                }
                for (int i = 0; i < numStrings; i++)
                    StringList.Add(reader.ReadZeroTerminatedString());

                for (int i = 0; i < numFiles; i++) {
                    string ext = StringList[Blocks[i].SourceIndex];
                    byte unkFlag = (byte)(Blocks[i].Flags >> 24 & 0xFF);
                    Blocks[i].FileName = $"Blocks{ext}/Block{i}_{Blocks[i].SourceType}_{unkFlag}{ext}";
                }
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new FileWriter(stream))
            {
                writer.SetByteOrder(false);
                writer.Write(0xA9F32458);
                writer.Write(HeaderFlags);
                writer.Write(IsCompressed);
                writer.Write((byte)0); //padding
                long maxValuePos = writer.Position;
                writer.Write(Blocks.Max(x => x.CompressedSize));
                writer.Write((byte)Blocks.Count);
                writer.Write((byte)ChunkInfos.Count);
                writer.Write((byte)StringList.Count);
                writer.Write((byte)0); //padding
            }
        }
    }
}
