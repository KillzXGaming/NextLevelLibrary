using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Toolbox.Core;
using System.IO;
using Toolbox.Core.IO;
using Toolbox.Core.Imaging;

namespace NextLevelLibrary.LM3
{
    public class TextureChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Hash;
            public ushort Width;
            public ushort Height;
            public byte Unknown;
            public byte Padding;
            public byte ArrayCount;
            public byte Unknown2;
            public byte Format;
            public byte Unknown3;
            public ushort Unknown4;
        }

        public static TextureWrapper Read(DATA_Parser.FileEntry file)
        {
            var imageChunk = file.ChunkEntry.GetChunk(ChunkDataType.TextureData);
            var headerChunk = file.ChunkEntry.GetChunk(ChunkDataType.TextureHeader);
            var textureHeader = headerChunk.ReadStruct<Header>();
            return new TextureWrapper(textureHeader, imageChunk.Data);
        }

        public class TextureWrapper : STGenericTexture
        {
            public Header Header { get; set; }
            public Stream ImageData { get; set; }

            public TextureWrapper(Header header, Stream imageData)
            {
                Header = header;
                ImageData = imageData;

                Name = Hashing.CreateHashString(header.Hash);

                Width = header.Width;
                Height = header.Height;
                ArrayCount = (uint)(header.ArrayCount > 0 ? header.ArrayCount : 1);
                MipCount = 1;
                Platform = new SwitchSwizzle(FormatList[header.Format]);
                Runtime.TextureCache.Add(this);
            }

            public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0) {
                return ImageData.ReadAllBytes();
            }

            public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
            {
                throw new NotImplementedException();
            }

            public Dictionary<int, TexFormat> FormatList = new Dictionary<int, TexFormat>()
            {
                { 0x0, TexFormat.RGBA8_UNORM },
                { 0x1, TexFormat.RGBA8_SRGB },
                { 0x5, TexFormat.RGBA8_SRGB },
                { 0xD, TexFormat.RGBA8_SRGB },
                { 0xE, TexFormat.RGBA8_SRGB },
                { 0x11, TexFormat.BC1_UNORM },
                { 0x12, TexFormat.BC1_SRGB },
                { 0x13, TexFormat.BC2_UNORM },
                { 0x14, TexFormat.BC3_UNORM },
                { 0x15, TexFormat.BC4_UNORM },
                { 0x16, TexFormat.BC5_SNORM },
                { 0x17, TexFormat.BC6H_UF16 },
                { 0x18, TexFormat.BC7_UNORM },
                { 0x19, TexFormat.ASTC_4x4_UNORM },
                { 0x1A, TexFormat.ASTC_5x4_UNORM },
                { 0x1B, TexFormat.ASTC_5x5_UNORM },
                { 0x1C, TexFormat.ASTC_6x5_UNORM },
                { 0x1D, TexFormat.ASTC_6x6_UNORM },
                { 0x1E, TexFormat.ASTC_8x5_UNORM },
                { 0x1F, TexFormat.ASTC_8x6_UNORM },
                { 0x20, TexFormat.ASTC_8x8_UNORM },
            };
        }
    }
}
