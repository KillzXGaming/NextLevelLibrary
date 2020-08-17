using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core.IO;

namespace NextLevelLibrary.MarioStrikers
{
    public class SANIM_Parser
    {
        public List<AnimationHeader> Animations = new List<AnimationHeader>();

        public SANIM_Parser(Stream stream)
        {
            using (var reader = new FileReader(stream)) {
                reader.SetByteOrder(true);

                while (!reader.EndOfStream) {
                    ReadChunkHeader(reader);
                }
            }
            foreach (var anim in Animations)
            {
                for (int i = 0; i < anim.TrackCount; i++)
                {
                    string param = "";
                    if (anim.Parameters1.Length > 0) param += $"p1 {anim.Parameters1[i]}\n";
                    if (anim.Parameters2.Length > 0) param += $"p2 {anim.Parameters2[i]}\n";
                    if (anim.Parameters3.Length > 0) param += $"p3 {anim.Parameters3[i]}\n";
                    if (anim.Parameters4.Length > 0) param += $"p4 {anim.Parameters4[i]}\n";
                    //  if (anim.Parameters5.Length > 0) param += $"p5 {anim.Parameters5[i]}\n";
                    // if (anim.Parameters6.Length > 0) param += $"p6 {anim.Parameters6[i]}\n";
                }
            }
        }

        public enum ChunkTypes
        {
           AnimStart = 0x7000,
           AnimHeader =  0x7001,
           AnimName = 0x7002,
           //These parameters are usually empty uint32s per bone.
           TrackParam1 = 0x7003,
           TrackParam2 = 0x7004,
           TrackParam3 = 0x7005,
           TrackParam4 = 0x7006,

           //These appear only once in an animation, length of the frame count
           UnknownKeyData = 0x7007, //1 short
           UnknownKeyData2 = 0x7008, //3 floats. 

           //Track data used per bone
           TrackDataStart = 0x7100,
           RotationKey = 0x7101, //Shorts. Can vary quat or euler
           TranslationKey = 0x7102, //3 Floats
           IndexedData = 0x7103,
        }

        private AnimationHeader CurrentAnimation;

        private void ReadChunkHeader(FileReader reader)
        {
            ushort flags = reader.ReadUInt16();
            ushort magic = reader.ReadUInt16();
            uint size = reader.ReadUInt32();

            Console.WriteLine($"ChunkTypes {(ChunkTypes)magic}");

            long pos = reader.Position;
            switch ((ChunkTypes)magic)
            {
                case ChunkTypes.AnimStart:
                    CurrentAnimation = new AnimationHeader();
                    Animations.Add(CurrentAnimation);
                    while (reader.Position < pos + size) {
                        ReadChunkHeader(reader);
                    }
                    break;
                case ChunkTypes.AnimHeader:
                    CurrentAnimation.ReadHeader(reader);
                    break;
                case ChunkTypes.AnimName:
                    CurrentAnimation.Name = reader.ReadString((int)size, true);
                    Console.WriteLine($"AnimName {CurrentAnimation.Name}");
                    break;
                case ChunkTypes.TrackParam1:
                    CurrentAnimation.Parameters1 = reader.ReadUInt32s((int)size / 4);
                    break;
                case ChunkTypes.TrackParam2:
                    CurrentAnimation.Parameters2 = reader.ReadUInt32s((int)size / 4);
                    break;
                case ChunkTypes.TrackParam3:
                    CurrentAnimation.Parameters3 = reader.ReadUInt32s((int)size / 4);
                    break;
                case ChunkTypes.TrackParam4:
                    CurrentAnimation.Parameters4 = reader.ReadUInt32s((int)size / 4);
                    break;
                case ChunkTypes.UnknownKeyData:
                    reader.ReadInt16s((int)size / 2);
                    break;
                case ChunkTypes.UnknownKeyData2:
                    reader.ReadSingles((int)size / 4);
                    break;
                //Data sections
                //These are sub sections of the track data start
                case ChunkTypes.TrackDataStart:
                    while (reader.Position < pos + size) {
                        ReadChunkHeader(reader);
                    }
                    break;
                case ChunkTypes.RotationKey:
                    for (int i = 0; i < size / 6; i++)
                    {
                        float X = reader.ReadInt16();
                        float Y = reader.ReadInt16();
                        float Z = reader.ReadInt16();
                    }
                    break;
                case ChunkTypes.TranslationKey:
                    for (int i = 0; i < size / 12; i++)
                    {
                        float X = reader.ReadSingle();
                        float Y = reader.ReadSingle();
                        float Z = reader.ReadSingle();

                       // Console.WriteLine($"key {i} v0 {v0} s0 {s0} s1 {s1}");
                    }
                    break;
                case ChunkTypes.IndexedData:
                    break;
            }
            reader.SeekBegin(pos + size);
            reader.Align(4);
        }

        public class AnimationHeader
        {
            public uint NameHash { get; set; }
            public uint FrameCount { get; set; }
            public uint TrackCount { get; set; }

            public string Name { get; set; }

            //Parameters which should all equal the track count
            //These are typically 0 and unused.
            public uint[] Parameters1 { get; set; } = new uint[0];
            public uint[] Parameters2 { get; set; } = new uint[0];
            public uint[] Parameters3 { get; set; } = new uint[0];
            public uint[] Parameters4 { get; set; } = new uint[0];


            public AnimationHeader() {}

            public void ReadHeader(FileReader reader)
            {
                reader.ReadUInt32(); //Always zero
                NameHash = reader.ReadUInt32();
                FrameCount = reader.ReadUInt32();
                TrackCount = reader.ReadUInt32();
            }
        }
    }
}
