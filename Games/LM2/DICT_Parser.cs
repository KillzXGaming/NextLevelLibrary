using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace NextLevelLibrary.LM2
{
    public class DICT_Parser : IDictionaryData
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ChunkInfo
        {
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
        }

        public IEnumerable<Block> BlockList => Blocks;
        public bool BlocksCompressed => IsCompressed;

        public List<ChunkInfo> ChunkInfos = new List<ChunkInfo>();
        public List<Block> Blocks = new List<Block>();
        public List<string> StringList = new List<string>();

        public ushort HeaderFlags = 0x4001;

        public bool IsCompressed = false;

        public string FilePath { get; set; }

        byte[] Unknowns { get; set; }

        public DICT_Parser(Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                uint Identifier = reader.ReadUInt32();
                HeaderFlags = reader.ReadUInt16();
                IsCompressed = reader.ReadByte() == 1;
                reader.ReadByte(); //Padding
                uint numFiles = reader.ReadUInt32();
                uint SizeLargestFile = reader.ReadUInt32();
                byte FileTableCount = reader.ReadByte(); 
                reader.ReadByte(); //Padding
                byte numChunkInfos = reader.ReadByte();
                byte numStrings = reader.ReadByte();
                ChunkInfos = reader.ReadMultipleStructs<ChunkInfo>(numChunkInfos);
                Unknowns = reader.ReadBytes((int)numFiles);
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
                    if (resourceFlag == 0x08 && resourceFlag2 == 1)
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
                    Blocks[i].FileName = $"Blocks{ext}/Block{i}_{Blocks[i].SourceType}{ext}";
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
                writer.Write(Blocks.Count);
                long maxValuePos = writer.Position;
                writer.Write(Blocks.Max(x => x.CompressedSize));
                writer.Write((byte)1);
                writer.Write((byte)0);
                writer.Write((byte)ChunkInfos.Count);
                writer.Write((byte)StringList.Count);
                foreach (var info in ChunkInfos)
                    writer.WriteStruct(info);
                writer.Write(Unknowns);
                for (int i = 0; i < Blocks.Count; i++)
                {
                    writer.Write(Blocks[i].Offset);
                    writer.Write(Blocks[i].DecompressedSize);
                    writer.Write(Blocks[i].CompressedSize);
                    writer.Write(Blocks[i].Flags);
                }
                foreach (var str in StringList)
                    writer.WriteString(str);
            }
        }
    }
}
