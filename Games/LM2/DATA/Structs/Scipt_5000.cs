using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace NextLevelLibrary.LM2
{
    public class SciptChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Element
        {
            public uint Hash;
            public uint Index;
            public ushort Unknown2;
            public ushort Unknown3;

            public override string ToString()
            {
                return $"{Hashing.CreateHashString(Hash)} {Index} {Unknown2} {Unknown3}";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Element2
        {
            public uint Hash;
            public uint Unknown;
            public uint Index;

            public override string ToString()
            {
                return $"{Hashing.CreateHashString(Hash)} {Index} {Unknown}";
            }
        }

        public class ScriptElementBundle
        {
            public uint[] Hashes { get; set; }
        }

        public static void Read(List<ChunkTable.ChunkDataEntry> chunkList)
        {
            List<Element> elements = new List<Element>();
            List<ScriptElementBundle> elementBundles = new List<ScriptElementBundle>();

            for (int i = 0; i < chunkList.Count; i++)
            {
                var chunk = chunkList[i];
                switch (chunk.ChunkType)
                {
                    case ChunkDataType.ScriptFileHash:
                        using (var reader = new FileReader(chunk.Data, true))
                        {
                            uint fileHash = reader.ReadUInt32();
                            Console.WriteLine(fileHash);
                        }
                        break;
                    case ChunkDataType.ScriptTable:
                        uint numElements = (uint)chunk.Data.Length / 12;
                        elements.AddRange(chunk.ReadStructs<Element>(numElements));
                        break;
                    case (ChunkDataType)0x5011:
                        using (var reader = new FileReader(chunk.Data, true))
                        {
                        /*    while (!reader.EndOfStream) {
                             //   uint count = reader.ReadUInt32();
                             //   uint[] hashes = reader.ReadUInt32s((int)count);
                               // elementBundles.Add(new ScriptElementBundle() { Hashes = hashes });

                             //  foreach (var hash in hashes)
                                 //   Console.WriteLine(hash);
                            }*/
                        }
                        break;
                    case (ChunkDataType)0x5015:
                        uint numScripts = (uint)chunk.Data.Length / 12;
                        var e = chunk.ReadStructs<Element2>(numScripts);
                        break;
                }
            }

            for (int i = 0; i < chunkList.Count; i++)
            {
                var chunk = chunkList[i];
                switch (chunk.ChunkType)
                {
                    case ChunkDataType.ScriptData:
                        ParseScriptData(new FileReader(chunk.Data, true), elements);
                        break;
                }
            }
        }

        static void ParseScriptData(FileReader reader, List<Element> elements)
        {
            uint value = reader.ReadUInt32();
            uint sectionSize2 = reader.ReadUInt32();
            uint sectionSize1 = reader.ReadUInt32();
            uint stringTableSize = reader.ReadUInt32();

            uint[] data = reader.ReadUInt32s((int)sectionSize1 / 4);
            ushort[] data2 = reader.ReadUInt16s((int)sectionSize2 / 2);

            for (int i = 0; i < data2.Length; i++)
            {
                for (int j = 0; j < elements.Count; j++)
                {
                  //  if (elements[j].Index == i)
                  //      Console.WriteLine($"element_{j} {elements[j]}");
                }

                byte opcode = (byte)(data2[i] >> 8);
               // Console.WriteLine($"opcode {data2[i].ToString("X4")}");
            }

          /*  for (int i = 0; i < data.Length; i++)
            {
                if (DICT.GlobalFileList.ContainsKey(data[i])) {
                    Console.WriteLine($"SCRIPT FILE REF {16 + (i * 4)} {data[i]} type {DICT.GlobalFileList[data[i]].ChunkEntry.ChunkType}");
                }
            }*/

            reader.SeekBegin(16 + sectionSize1 + sectionSize2);
            while (!reader.EndOfStream)
            {
                string str = reader.ReadZeroTerminatedString();
              //  Console.WriteLine(str);
            }
        }

        public enum OPCODE
        {
            START = 0x38,
            END = 0x40,
        }
    }
}
