using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace NextLevelLibrary
{
    /// <summary>
    /// Loads uncompressed data and dumps the blocks into an uncompressed file on disk to save memory and speed up loading.
    /// </summary>
    public class DataCompressionCache
    {
        public static Stream LoadDumpedBlocks(IDictionaryData dict, List<Block> blocks, Stream stream, int index) {
            var block = blocks[index];

            var fileName = Path.GetFileNameWithoutExtension(dict.FilePath);
            var folderPath = Path.GetDirectoryName(dict.FilePath);
            var dir = Path.Combine(folderPath, fileName, "File_Data");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists($"{dir}/{fileName}_{index}.lm3"))
                GetDecompressedDataBlock(dict, block, stream).SaveToFile($"{dir}/{fileName}_{index}.lm3");

            return new FileStream($"{dir}/{fileName}_{index}.lm3", FileMode.Open, FileAccess.Read);
        }

        //Reads a block from the dictionary and decompresses.
        static Stream GetDecompressedDataBlock(IDictionaryData dict, Block block, Stream stream)
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
                    else //Unsupported compression. This should not happen.
                        return new MemoryStream();
                } //File is decompressed so check if it's in the range of the current data file.
                else if (block.Offset + block.DecompressedSize <= reader.BaseStream.Length)
                    return new SubStream(reader.BaseStream, block.Offset, block.DecompressedSize);
            }
            return new MemoryStream();
        }
    }
}
