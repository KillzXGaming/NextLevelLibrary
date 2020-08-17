using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace NextLevelLibrary.LM3
{
    public class SciptChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Element
        {
            public uint Hash;
            public uint Unknown;
            public ushort Unknown2;
            public ushort Unknown3;
        }

        public static void Read(List<ChunkTable.ChunkDataEntry> chunkList)
        {
            List<Element> elements = new List<Element>();

            for (int i = 0; i < chunkList.Count; i++) {
                var chunk = chunkList[i];
                switch (chunk.ChunkType)
                {
                    case ChunkDataType.ScriptInfo:
                        break;
                    case ChunkDataType.ScriptTable:
                        uint numElements = (uint)chunk.Data.Length / 12;
                        elements = chunk.ReadStructs<Element>(numElements);
                        foreach (var elm in elements) {
                           // Console.WriteLine($"element {Hashing.CreateHashString(elm.Hash)} {elm.Unknown} {elm.Unknown2} {elm.Unknown3}");
                        }
                        break;
                    case ChunkDataType.ScriptData:
                        ParseScriptData(new FileReader(chunk.Data, true));
                        break;
                }
            }
        }

        static void ParseScriptData(FileReader reader)
        {
            uint value = reader.ReadUInt32();
            uint value2 = reader.ReadUInt32();
            uint sectionSize1 = reader.ReadUInt32();
            uint sectionSize2 = reader.ReadUInt32();
            uint stringTableSize = reader.ReadUInt32();

            uint[] data = reader.ReadUInt32s((int)sectionSize1 / 4);
            for (int i = 0; i < data.Length; i++)
            {
                if (DICT.GlobalFileList.ContainsKey(data[i]))
                {
                   // Console.WriteLine($"SCRIPT FILE REF {reader.Position} {data[i]} type {DICT.GlobalFileList[data[i]].ChunkEntry.ChunkType}");
                }
            }

            reader.SeekBegin(20 + sectionSize1 + sectionSize2);
            while (!reader.EndOfStream)
            {
                string str = reader.ReadZeroTerminatedString();
                Console.WriteLine(str);
            }
        }
    }
}
