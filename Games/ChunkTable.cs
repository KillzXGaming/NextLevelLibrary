using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace NextLevelLibrary
{
    public class ChunkTable
    {
        private const short ChunkInfoIdenfier = 0x1301;

        public List<ChunkEntry> Files = new List<ChunkEntry>();
        public List<ChunkDataEntry> DataEntries = new List<ChunkDataEntry>();

        public class ChunkEntry : Chunk
        {
            public uint ChunkSize;
            public uint ChunkOffset;
            public ChunkFileType ChunkType;
            public ushort Flags;
            public uint Flags2;
            public uint Flags3;

            public bool HasSubData => (Flags >> 12) > 2;

            public uint BlockIndex = 0;
            public uint FileBlockIndex = 0;

            public int BeginIndex;

            public List<ChunkDataEntry> SubData = new List<ChunkDataEntry>();

            public ChunkDataEntry GetChunk(ChunkDataType type)
            {
                for (int i = 0; i < SubData.Count; i++) {
                    if (SubData[i].ChunkType == type)
                        return SubData[i];
                }
                return null;
            }

            private Stream data;
            public Stream Data
            {
                get { return data; }
                set { data = value; }
            }
        }

        public class ChunkDataEntry : Chunk
        {
            public ChunkDataType ChunkType;
            public ushort Flags;
            public uint ChunkSize;
            public uint ChunkOffset;
            public uint BlockIndex = 0;

            public bool HasSubData => (Flags >> 12) > 2;

            public List<ChunkDataEntry> SubData = new List<ChunkDataEntry>();

            public Stream Data;

            public T ReadStruct<T>()
            {
                using (var reader = new FileReader(Data, true)) {
                    return reader.ReadStruct<T>();
                }
            }

            public List<T> ReadStructs<T>(uint count)
            {
                using (var reader = new FileReader(Data, true)) {
                    return reader.ReadMultipleStructs<T>(count);
                }
            }

            public List<T> ReadPrimitive<T>(uint count)
            {
                T[] instace = new T[count];
                using (var reader = new FileReader(Data, true)) {
                    for (int i = 0; i < count; i++)
                    {
                        object value = null;
                        if (typeof(T) == typeof(uint)) value = reader.ReadUInt32();
                        else if (typeof(T) == typeof(int)) value = reader.ReadInt32();
                        else if (typeof(T) == typeof(short)) value = reader.ReadInt16();
                        else if (typeof(T) == typeof(ushort)) value = reader.ReadUInt16();
                        else if (typeof(T) == typeof(float)) value = reader.ReadSingle();
                        else if (typeof(T) == typeof(bool)) value = reader.ReadBoolean();
                        else if (typeof(T) == typeof(sbyte)) value = reader.ReadSByte();
                        else if (typeof(T) == typeof(byte)) value = reader.ReadByte();
                        else
                            throw new Exception("Unsupported primitive type! " + typeof(T));

                        instace[i] = (T)value;
                    }
                }
                return instace.ToList();
            }
        }

        public class Chunk
        {

        }

        public ChunkTable(Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                Read(reader);
            }
        }

        void Read(FileReader reader) {
            //File is empty so return
            if (reader.BaseStream.Length <= 4)
                return;

            Dictionary<int, Chunk> globalChunkList = new Dictionary<int, Chunk>();

            int globalIndex = 0;

            reader.SetByteOrder(false);
            while (reader.Position <= reader.BaseStream.Length - 12) {
                //Read through all sections that use an identifier
                //These sections determine when a file is used or else using raw data.
                ushort identifier = reader.ReadUInt16();
                if (identifier == ChunkInfoIdenfier)
                {
                    //Skip padding
                    ushort flag = reader.ReadUInt16();

                    ChunkEntry entry = new ChunkEntry();
                    entry.ChunkSize = reader.ReadUInt32();
                    entry.ChunkOffset = reader.ReadUInt32();
                    entry.ChunkType = (ChunkFileType)reader.ReadUInt16();
                    entry.Flags = reader.ReadUInt16();
                    entry.Flags2 = reader.ReadUInt32(); //Child Count or File Size
                    entry.Flags3 = reader.ReadUInt32(); //Child Start Index or File Offset     

                    Files.Add(entry);
                    globalChunkList.Add(globalIndex, entry);

                    //File entries shift global index by 2
                    globalIndex += 2;

                    //Additional chunk entry
                    if ((int)entry.ChunkType == 0x11) //This file seems to use same hash as some of the model files.
                    {
                        ChunkEntry secondaryEntry = new ChunkEntry();
                        secondaryEntry.Flags2 = reader.ReadUInt32(); //Child Count or File Size
                        secondaryEntry.Flags3 = reader.ReadUInt32(); //Child Start Index or File Offset  
                        secondaryEntry.ChunkType = (ChunkFileType)reader.ReadUInt16();
                        secondaryEntry.Flags = reader.ReadUInt16();
                        Files.Add(secondaryEntry);
                        globalChunkList.Add(globalIndex, entry);

                        //Extra entries shift global index by 1
                        globalIndex += 1;
                    }
                    //Extension to the existing file entry? 
                    //Possibly includes both sub chunks and data offset/size chunks in one
                    if ((int)entry.ChunkType == 0x20)
                    {
                        var Flags2 = reader.ReadUInt32(); //Child Count or File Size
                        var Flags3 = reader.ReadUInt32(); //Child Start Index or File Offset  
                        var ChunkType = (ChunkFileType)reader.ReadUInt16();
                        var Flags = reader.ReadUInt16();

                       // //Extra entries shift global index by 1
                        globalIndex += 1;
                    }
                }
                else
                {
                    reader.Seek(-2);
                    ChunkDataEntry subEntry = new ChunkDataEntry();
                    subEntry.ChunkType = reader.ReadEnum<ChunkDataType>(false); //The type of chunk. 0x8701B5 for example for texture info
                    subEntry.Flags = reader.ReadUInt16();
                    subEntry.ChunkSize = reader.ReadUInt32();
                    subEntry.ChunkOffset = reader.ReadUInt32();

                    byte blockFlag = (byte)((subEntry.Flags >> 12));
                    if (blockFlag < 8)
                        subEntry.BlockIndex = blockFlag;

                    DataEntries.Add(subEntry);
                    globalChunkList.Add(globalIndex, subEntry);
                    globalIndex += 1;
                }
            }

            if (Files.Count == 0)
            {
                var file = new ChunkEntry();
                file.SubData.AddRange(DataEntries);
                Files.Add(file);
            }

            for (int i = 0; i < DataEntries.Count; i++)
            {
                if (DataEntries[i].ChunkType == ChunkDataType.BoneStart ||
                    DataEntries[i].ChunkType == (ChunkDataType)0xC800 ||
                    DataEntries[i].ChunkType == (ChunkDataType)0x6200 ||
                    DataEntries[i].ChunkType == (ChunkDataType)0x6500) {                    
                //    Console.WriteLine($"BONEFLAGS {DataEntries[i].ChunkFlags} {((DataEntries[i].ChunkFlags >> 12) > 2)}");
                    for (int f = 0; f < DataEntries[i].ChunkSize; f++) {
                        DataEntries[i].SubData.Add((ChunkDataEntry)globalChunkList[(int)DataEntries[i].ChunkOffset + f]);
                    }
                }
            }

            for (int i = 0; i < Files.Count; i++)
            {
                if (Files[i].HasSubData && globalChunkList.ContainsKey((int)Files[i].Flags3)) {
                    Files[i].BeginIndex = (int)Files[i].Flags3;

                    for (int f = 0; f < Files[i].Flags2; f++) {
                        Files[i].SubData.Add((ChunkDataEntry)globalChunkList[Files[i].BeginIndex + f]);
                    }
                }
            }
        }
    }
}
