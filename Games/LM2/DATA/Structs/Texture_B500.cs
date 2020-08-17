using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Toolbox.Core;
using System.IO;
using Toolbox.Core.IO;
using Toolbox.Core.Imaging;

namespace NextLevelLibrary.LM2
{
    public class TextureChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint ImageSize;
            public uint Hash;
            public uint Padding;
            public uint Unknown2;
            public ushort Width;
            public ushort Height;
            public byte Unknown3;
            public ushort Unknown4;
            public byte MipCount;
            public uint Unknown6;
            public uint Unknown7;
            public uint Unknown8;
            public uint Unknown9;
            public uint Unknown10;
            public byte Format;
            public byte Unknown11;
            public byte Unknown12;
            public byte Unknown13;
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
                ArrayCount = 1;
                MipCount = 1;
                Platform = new CTRSwizzle((CTR_3DS.PICASurfaceFormat)header.Format)
                {
                    SwizzleMode = CTR_3DS.Orientation.Transpose,
                };

                Runtime.TextureCache.Add(this);
            }

            public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
            {
                return ImageData.ReadAllBytes();
            }

            public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
            {
                throw new NotImplementedException();
            }
        }
    }
}
