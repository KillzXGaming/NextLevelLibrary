using Syroot.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace NextLevelLibrary
{
    public class Block : ArchiveFileInfo
    {
        public Stream CompressedData { get; set; }

        public override Stream FileData { get => CompressedData; set => CompressedData = value; }

        public IDictionaryData Dictionary { get; set; }

        public DATA_Parser DataParser { get; set; }

        public uint Offset;
        public uint DecompressedSize;
        public uint CompressedSize;

        public int Index { get; set; }

        /// <summary>
        /// The flags of the block.
        /// </summary>
        public uint Flags;

        public Block(int index) { 
            Index = index;
        }

        /// <summary>
        /// The index of the data source file.
        /// Most external sources may not exist for debug purposes and can be skipped
        /// </summary>
        public byte SourceIndex { get; set; }

        public ResourceType SourceType { get; set; }

        public override Stream DecompressData(Stream compressed) {
            if (DecompressedSize == 0) return compressed;

            if (Dictionary.BlocksCompressed)
                return new MemoryStream(STLibraryCompression.ZLIB.Decompress(ReadBytes()));
            else
                return compressed;
        }

        public override Stream CompressData(Stream decompressed)
        {
            byte[] decomp = decompressed.ToArray();
            byte[] comp = STLibraryCompression.ZLIB.Compress(decomp);
            CompressedSize = (uint)comp.Length;
            DecompressedSize = (uint)decomp.Length;
            return new MemoryStream(comp);
        }

        private byte[] ReadBytes()
        {
            using (var reader = new FileReader(CompressedData, true)) {
                return reader.ReadBytes((int)reader.BaseStream.Length);
            }
        }
    }
}
