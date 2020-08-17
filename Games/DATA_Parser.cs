using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace NextLevelLibrary
{
    //Todo this chunk system is very messy. I have no idea how they assign blocks properly.
    public class DATA_Parser
    {
        public List<FileEntry> Files = new List<FileEntry>();

        public ChunkTable Table { get; set; }

        public DICT.GameVersion Version { get; set; }

        public IDictionaryData Dictionary { get; set; }

        public DATA_Parser(Stream stream, DICT.GameVersion version, IDictionaryData dict)
        {
            Version = version;
            Dictionary = dict;

            foreach (var block in dict.BlockList)
            {
                block.DataParser = this;

                uint size = dict.BlocksCompressed ? block.CompressedSize : block.DecompressedSize;
                //Some cases the block sizes are too big? 
                if (block.Offset + size > stream.Length)
                    continue;

                block.Dictionary = dict;
                block.CompressedData = new SubStream(stream, block.Offset, size);
            }

            var blocks = dict.BlockList.ToList();

            Stream[] blockList = new Stream[100];
            if (version == DICT.GameVersion.LM3)
            {
                blockList[0] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 0);
                blockList[52] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 52);
                blockList[53] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 53);
                blockList[54] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 54);
                blockList[55] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 55);
                blockList[58] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 58);

                blockList[63] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 63);
                blockList[64] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 64);
                blockList[65] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 65);
                blockList[68] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 68);
                blockList[69] = DataCompressionCache.LoadDumpedBlocks(dict, blocks, stream, 69);
            }
            else
            {
                blockList[0] = GetDecompressedDataBlock(dict, blocks[0], stream);
                blockList[2] = GetDecompressedDataBlock(dict, blocks[2], stream);
                blockList[3] = GetDecompressedDataBlock(dict, blocks[3], stream);
            }

            var table = new ChunkTable(blockList[0]);
            Table = table;

            for (int i = 0; i < table.Files.Count; i++)
            {
                var file = table.Files[i];
                Stream fileTableBlock = null;
                Stream fileTableBlock2 = null;

                if (version == DICT.GameVersion.LM3)
                {
                    fileTableBlock = blockList[52];
                    fileTableBlock2 = blockList[63];
                }
                else
                {
                    fileTableBlock = blockList[2];
                    fileTableBlock2 = blockList[2];
                }

                if (file.ChunkType == ChunkFileType.Texture)
                    Files.Add(ParseFileHeaders(fileTableBlock2, file));
                else if (file.ChunkType == ChunkFileType.FileTable)
                    Files.Add(ParseFileHeaders(fileTableBlock, file));
                else if (file.ChunkType == ChunkFileType.Script)
                    Files.Add(ParseFileHeaders(fileTableBlock2, file));
                else if (file.ChunkType == ChunkFileType.ClothPhysics)
                    Files.Add(ParseFileHeaders(fileTableBlock2, file));
                else if (file.ChunkType == (ChunkFileType)0x6510)
                    Files.Add(ParseFileHeaders(fileTableBlock2, file));
                else
                    Files.Add(ParseFileHeaders(fileTableBlock, file));

                if (file.SubData.Count == 0)
                {
                    if (version == DICT.GameVersion.LM3) {
                        file.Data = blockList[53];

                        if (file.ChunkType == ChunkFileType.FileTable)
                            file.Data = blockList[58];
                        if (file.ChunkType == (ChunkFileType)0x7200)
                            file.Data = blockList[54];
                        if (file.ChunkType == (ChunkFileType)0xF000)
                            file.Data = blockList[69];
                        if (file.ChunkType == (ChunkFileType)0x4300)
                            file.Data = blockList[53];
                        if (file.ChunkType == ChunkFileType.MessageData)
                            file.Data = blockList[69];
                    }
                    else {
                        file.Data = blockList[3];
                    }

                    if (file.Data != null && file.Flags3 + file.Flags2 <= file.Data.Length)
                        file.Data = new SubStream(file.Data, file.Flags3, file.Flags2);
                }
            }

            foreach (var entry in table.DataEntries)
            {
                if (version == DICT.GameVersion.LM3)
                {
                    if (entry.ChunkType == ChunkDataType.TextureData)
                        entry.Data = blockList[65];

                    if (ChunkBlockFlags.ContainsKey(entry.Flags))
                    {
                        var blockType = ChunkBlockFlags[entry.Flags];
                        entry.Data = blockList[blockType];

                        entry.BlockIndex = blockType;
                    }
                    else
                        continue;

                    switch (entry.ChunkType)
                    {
                        case (ChunkDataType)0xA201:
                        case (ChunkDataType)0xA202:
                        case (ChunkDataType)0xA203:
                        case (ChunkDataType)0xA204:
                        case (ChunkDataType)0xA205:
                        case (ChunkDataType)0xA206:
                        case (ChunkDataType)0xA207:
                            entry.Data = blockList[64];
                            break;
                    }
                }
                else
                {
                    if (entry.BlockIndex == 0)
                        entry.Data = blockList[2];
                    if (entry.BlockIndex == 1)
                        entry.Data = blockList[3];
                }

                if (entry.ChunkOffset + entry.ChunkSize <= entry.Data.Length)
                    entry.Data = new SubStream(entry.Data, entry.ChunkOffset, entry.ChunkSize);
            }
        }

        public void Save(Stream stream)
        {

        }

        //A list of all the flags used and block indices to use for LM3. 
        private static Dictionary<uint, uint> ChunkBlockFlags = new Dictionary<uint, uint>()
        {
            { 128, 52 },
            { 257, 52 },
            { 129, 52 },
            { 192, 53 },
            { 193, 52 },
            { 194, 53 },
            { 2241, 53 },
            { 2242, 53 },
            { 2264, 53 },
            { 2305, 53 },
            { 4353, 54 },
            { 6721, 55 },
            { 8386, 68 },
            { 14530, 63 },
            { 14528, 63 },
            { 14529, 63 },
            { 16577, 64 },
            { 16641, 64 },
            { 16642, 64 },
            { 16578, 64 },
            { 21057, 65 },
            { 32961, 65 },
            { 35073, 65 },
            { 35009, 65 },
        };

        private FileEntry ParseFileHeaders(Stream stream, ChunkTable.ChunkEntry chunkEntry)
        {
            using (var reader = new FileReader(stream, true))
            {
                if (reader.BaseStream.Length < chunkEntry.ChunkOffset + 8)
                    return new FileEntry() { ChunkEntry = chunkEntry };

                reader.SeekBegin(chunkEntry.ChunkOffset);
                var file = new FileEntry();
                if (chunkEntry.ChunkSize == 8)
                {
                    file.Magic = reader.ReadUInt32();
                    file.Hash = reader.ReadUInt32();
                }
                file.ChunkEntry = chunkEntry;
                return file;
            }
        }

        public class FileEntry
        {
            public uint Magic { get; set; }
            public uint Hash { get; set; }

            public ChunkTable.ChunkEntry ChunkEntry;
        }

        //Reads a block from the dictionary and decompresses.
        private Stream GetDecompressedDataBlock(IDictionaryData dict, Block block, Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                if (block.Offset > reader.BaseStream.Length || block.DecompressedSize == 0)
                    return new MemoryStream();

                reader.SeekBegin(block.Offset);
                //Check the dictionary if the files are compressed
                if (dict.BlocksCompressed)
                {
                    //Check the magic to see if it's zlib compression
                    ushort Magic = reader.ReadUInt16();
                    bool IsZLIP = Magic == 0x9C78 || Magic == 0xDA78;
                    reader.SeekBegin(block.Offset);

                    if (IsZLIP)
                        return new MemoryStream(STLibraryCompression.ZLIB.Decompress(
                            reader.ReadBytes((int)block.CompressedSize)));
                    else //Unknown compression so skip it.
                        return new MemoryStream();
                } //File is decompressed so check if it's in the range of the current data file.
                else if (block.Offset + block.DecompressedSize <= reader.BaseStream.Length)
                    return new SubStream(reader.BaseStream, block.Offset, block.DecompressedSize);
            }
            return new MemoryStream();
        }
    }
}
