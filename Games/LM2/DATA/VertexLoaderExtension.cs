using System;
using System.Collections.Generic;
using OpenTK;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace NextLevelLibrary
{
    public static class VertexLoaderExtension
    {
        public static STVertex ReadVertexLayout(this FileReader reader, uint hash)
        {
            switch (hash)
            {
                case 0xC88C1762:
                case 0x72D28D0D:
                case 0x11E26127:
                case 0x679BEB7C:
                case 0x8980687F:
                case 0xB3F4492E:
                case 0x0FFA5BDE:
                    return ReadLayout1(reader, hash);
                case 1879155460:
                    return ReadLayout1(reader, hash);
                case 0x333626D9:
                case 3767596423:
                case 2860802050:
                case 1968188335:
                    return ReadLayout2(reader, hash);
                case 3695673818:
                    return ReadLayout3(reader, hash);
                case 0x5576A693:
                case 0xDF24890D:
                case 0xF4F13EB1:
                case 0xFADABB22:
                case 0x87C2B716:
                case 0x1090E6EB:
                case 0xA856FBF7:
                case 0xF07FC596:
                case 0xDDCC31B7:
                case 3276728684:
                case 2808796972:
                    return ReadLayout3(reader, hash);
                case 0x4821B2DF:
                    return ReadLayout4(reader, hash);
                case 170572476:
                    return ReadLayout5(reader, hash);
                case 3483481649:
                    return ReadLayout6(reader, hash);
                default:
                    throw new Exception("Unsupported vertex layout!");
            }
        }

        private static STVertex ReadLayout1(this FileReader reader, uint hash) {
            uint stride = GetStride(hash);

            STVertex vertex = new STVertex();
            vertex.TexCoords = new Vector2[1];

            long pos = reader.Position;

            vertex.Position = new Vector3(
                           UShortToFloatDecode(reader.ReadInt16()),
                           UShortToFloatDecode(reader.ReadInt16()),
                           UShortToFloatDecode(reader.ReadInt16()));

            vertex.Normal = new Vector3(
                reader.ReadSByte() / 255f,
                reader.ReadSByte() / 255f,
                reader.ReadSByte() / 255f);
            byte boneIndex1 = reader.ReadByte();
            vertex.TexCoords[0] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);

            if (stride >= 22) {
                byte boneIndex2 = reader.ReadByte();
                byte boneIndex3 = reader.ReadByte();
                ushort[] weights = reader.ReadUInt16s(3);

                vertex.BoneIndices.Add(boneIndex2 / 3);
                vertex.BoneIndices.Add(boneIndex3 / 3);
                if (weights[2] != 0)
                    vertex.BoneIndices.Add(boneIndex1 / 3);

                vertex.BoneWeights.Add(weights[0] / 16384.0f);
                vertex.BoneWeights.Add(weights[1] / 16384.0f);
                if (weights[2] != 0)
                    vertex.BoneWeights.Add(weights[2] / 16384.0f);
            }

            if (stride == 26)
            {

            }

            return vertex;
        }

        private static STVertex ReadLayout2(this FileReader reader, uint hash)
        {
            uint stride = GetStride(hash);

            STVertex vertex = new STVertex();
            vertex.TexCoords = new Vector2[2];
            if (stride > 20)
                vertex.Colors = new Vector4[1];

            vertex.Position = new Vector3( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            vertex.TexCoords[0] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);
            vertex.TexCoords[1] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);
            if (stride > 20)
            {
                vertex.Colors[0] = new Vector4(
                    reader.ReadByte() / 255.0f,
                    reader.ReadByte() / 255.0f,
                    reader.ReadByte() / 255.0f,
                    reader.ReadByte() / 255.0f);
            }

            return vertex;
        }

        private static STVertex ReadLayout3(this FileReader reader, uint hash)
        {
            uint stride = GetStride(hash);

            STVertex vertex = new STVertex();
            vertex.TexCoords = new Vector2[2];
            if (stride >= 0x1C)
                vertex.Colors = new Vector4[1];

            vertex.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            vertex.Normal = new Vector3(
                reader.ReadSByte() / 255f,
                reader.ReadSByte() / 255f,
                reader.ReadSByte() / 255f).Normalized();
            reader.ReadSByte();
            vertex.TexCoords[0] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);
            vertex.TexCoords[1] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);

            if (stride >= 0x1C)
                vertex.Colors[0] = new Vector4(
                reader.ReadByte() / 255.0f,
                reader.ReadByte() / 255.0f,
                reader.ReadByte() / 255.0f,
                reader.ReadByte() / 255.0f);
            return vertex;
        }

        private static STVertex ReadLayout4(this FileReader reader, uint hash)
        {
            uint stride = GetStride(hash);

            STVertex vertex = new STVertex();
            vertex.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            return vertex;
        }

        private static STVertex ReadLayout5(this FileReader reader, uint hash)
        {
            uint stride = GetStride(hash);

            STVertex vertex = new STVertex();
            vertex.TexCoords = new Vector2[2];
            vertex.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            vertex.Normal = new Vector3(
                reader.ReadSByte() / 255f,
                reader.ReadSByte() / 255f,
                reader.ReadSByte() / 255f).Normalized();
            reader.ReadSByte();
            vertex.TexCoords[0] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);
            vertex.TexCoords[1] = new Vector2(
                reader.ReadInt16() / 1024.0f,
                reader.ReadInt16() / 1024.0f);
            return vertex;
        }

        //Geometry shader effects ie uvslidingmaterialgs.
        private static STVertex ReadLayout6(this FileReader reader, uint hash)
        {
            //This requires geoemtry shader so skip it atm
            return new STVertex();

            uint stride = GetStride(hash);

            STVertex vertex = new STVertex();
            vertex.TexCoords = new Vector2[1];

            vertex.Position = new Vector3(
                                    reader.ReadSingle(),
                                    reader.ReadSingle(), 
                                    reader.ReadSingle());
            return vertex;
        }

        public static uint GetStride(uint hash)
        {
            switch (hash)
            {
                case 0xC88C1762:
                case 0x72D28D0D:
                    return 0x46;
                case 1879155460:
                    return 0x4A;
                case 170572476:
                    return 0x24;
                case 0x11E26127:
                case 0x679BEB7C:
                case 0x8980687F:
                case 0xB3F4492E:
                    return 0x16;
                case 0x0FFA5BDE:
                    return 0x1A;
                case 0x4821B2DF:
                case 3695673818:
                case 3767596423:
                case 2860802050:
                case 1968188335:
                    return 0x14;
                case 0x5576A693:
                    return 0x10;
                case 0xDF24890D:
                case 0xF4F13EB1:
                case 0xFADABB22:
                case 0x87C2B716:
                case 0x333626D9:
                    return 0x18;
                case 0x1090E6EB:
                case 0xA856FBF7:
                case 0xF07FC596:
                case 0xDDCC31B7:
                case 3276728684:
                case 2808796972:
                    return 0x1C;
                case 3483481649:
                    return 0xC;
                default:
                    return 0;
            }
        }

        private static float UShortToFloatDecode(short input)
        {
            float fraction = (float)BitConverter.GetBytes(input)[0] / (float)256;
            sbyte integer = (sbyte)BitConverter.GetBytes(input)[1];
            return integer + fraction;
        }
    }
}
