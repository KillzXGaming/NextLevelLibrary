using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Toolbox.Core.IO;
using Toolbox.Core.Animations;
using OpenTK;
using Toolbox.Core;
using System.Linq;

namespace NextLevelLibrary.LM3
{
    public class AnimationChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Padding;
            public ushort TrackCount;
            public ushort FrameCount;
            public float Duriation;
            public uint Padding2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Track
        {
            public uint Hash;
            public byte Index;
            public byte Unknown;
            public byte Type;
            public byte OpCode;
            public uint DataOffset;
        }

        public static STAnimation Read(Stream stream) {
            return Read(new FileReader(stream, true));
        }

        static STAnimation Read(FileReader reader) {
            AnimationFormat anim = new AnimationFormat();

            Dictionary<uint, AnimationGroup> groupList = new Dictionary<uint, AnimationGroup>();

            Header header = reader.ReadStruct<Header>();
            List<Track> tracks = reader.ReadMultipleStructs<Track>(header.TrackCount);

            anim.FrameCount = header.FrameCount;
            anim.FrameRate = header.FrameCount / header.Duriation;

            for (int i = 0; i < header.TrackCount; i++) {
                //Add and combine tracks by hash.
                if (!groupList.ContainsKey(tracks[i].Hash)) {
                    var newGroup = new AnimationGroup()
                    { Name = Hashing.CreateHashString(tracks[i].Hash), };

                    anim.AnimGroups.Add(newGroup);
                    groupList.Add(tracks[i].Hash, newGroup);
                }

                var group = groupList[tracks[i].Hash];

                reader.SeekBegin(tracks[i].DataOffset);
                if (tracks[i].Type == 0)
                    ParseScaleTrack(reader, header, tracks[i], group);
                else if (tracks[i].Type == 1)
                   ParseRotationTrack(reader, header, tracks[i], group);
                else if (tracks[i].Type == 3)
                   ParseTranslationTrack(reader, header, tracks[i], group);
            }

            return anim;
        }

        static void ParseScaleTrack(FileReader reader, Header header, Track track, AnimationGroup group)
        {

        }

        static void ParseTranslationTrack(FileReader reader, Header header, Track track, AnimationGroup group)
        {
            switch (track.OpCode)
            {
                case 0x06:
                    {
                        for (int f = 0; f < header.FrameCount; f++)
                        {
                            float frame = f;
                            float[] loc = reader.ReadSingles(3);
                            group.TranslateX.AddKey(frame, loc[0]);
                            group.TranslateY.AddKey(frame, loc[1]);
                            group.TranslateZ.AddKey(frame, loc[2]);
                        }
                    }
                    break;
                case 0x08:
                    {
                        for (int f = 0; f < header.FrameCount; f++)
                        {
                            float frame = f;
                            float[] loc = reader.ReadHalfSingles(3);
                            group.TranslateX.AddKey(frame, loc[0]);
                            group.TranslateY.AddKey(frame, loc[1]);
                            group.TranslateZ.AddKey(frame, loc[2]);
                        }
                    }
                    break;
                case 0x9:
                    {
                        uint count = reader.ReadUInt32();
                        for (int f = 0; f < count; f++)
                        {
                            float frame = (f * count) / header.FrameCount;
                            float[] loc = reader.ReadSingles(3);
                            group.TranslateX.AddKey(frame, loc[0]);
                            group.TranslateY.AddKey(frame, loc[1]);
                            group.TranslateZ.AddKey(frame, loc[2]);
                        }
                    }
                    break;
                case 0xA:
                    {
                        uint count = reader.ReadUInt16();
                        for (int f = 0; f < count; f++)
                        {
                            float frame = (f * count) / header.FrameCount;
                            float[] loc = reader.ReadHalfSingles(3);
                            group.TranslateX.AddKey(frame, loc[0]);
                            group.TranslateY.AddKey(frame, loc[1]);
                            group.TranslateZ.AddKey(frame, loc[2]);
                        }
                    }
                    break;
                case 0xB:
                    {
                        float[] loc = reader.ReadSingles(3);
                        group.TranslateX.AddKey(0, loc[0]);
                        group.TranslateY.AddKey(0, loc[1]);
                        group.TranslateZ.AddKey(0, loc[2]);
                    }
                    break;
                case 0xC:
                    {
                        float[] loc = reader.ReadHalfSingles(3);
                        group.TranslateX.AddKey(0, loc[0]);
                        group.TranslateY.AddKey(0, loc[1]);
                        group.TranslateZ.AddKey(0, loc[2]);
                    }
                    break;
                case 0xD:
                    {
                        uint axis = reader.ReadUInt32();
                        var unk = reader.ReadBytes(8);
                        uint count = reader.ReadUInt32();
                        for (int f = 0; f < count; f++)
                        {
                            float frame = (f * count) / header.FrameCount;
                            float value = reader.ReadSingle();

                            var loc = new Vector3(0, 0, 0);
                            if (axis == 0)
                                loc = new Vector3(value, 0, 0);
                            else if (axis == 1)
                                loc = new Vector3(0, value, 0);
                            else if (axis == 2)
                                loc = new Vector3(0, 0, value);

                            group.TranslateX.AddKey(0, loc.X);
                            group.TranslateY.AddKey(0, loc.Y);
                            group.TranslateZ.AddKey(0, loc.Z);
                        }
                    }
                    break;
                case 0xE:
                    {
                        uint unk1 = reader.ReadUInt32();
                        ushort unk2 = reader.ReadUInt16();
                        ushort count = reader.ReadUInt16();
                        for (int f = 0; f < count; f++)
                        {
                            float frame = (f * count) / header.FrameCount;
                            float positionX = reader.ReadHalfSingle();
                            var loc = new Vector3(positionX, 0, 0);
                            group.TranslateX.AddKey(0, loc.X);
                            group.TranslateY.AddKey(0, loc.Y);
                            group.TranslateZ.AddKey(0, loc.Z);
                        }
                    }
                    break;
            }
        }

        static void ParseRotationTrack(FileReader reader, Header header, Track track, AnimationGroup group)
        {
            switch (track.OpCode)
            {
                case 0x0F:  //4 Singles Frame Count
                {
                        for (int f = 0; f < header.FrameCount; f++) {
                            float frame = f;
                            float[] quat = reader.ReadSingles(4);

                            Quaternion quaternion = new Quaternion(quat[0], quat[1], quat[2], quat[3]);
                            group.RotateX.AddKey(frame, quaternion.X);
                            group.RotateY.AddKey(frame, quaternion.Y);
                            group.RotateZ.AddKey(frame, quaternion.Z);
                            group.RotateW.AddKey(frame, quaternion.W);
                        }
                    }
                    break;
                case 0x13: //4 Singles Custom Count
                    {
                        uint count = reader.ReadUInt32();
                        for (int f = 0; f < count; f++)
                        {
                            float frame = f;
                            float[] quat = reader.ReadSingles(4);
                            group.RotateX.AddKey(frame, quat[0]);
                            group.RotateY.AddKey(frame, quat[1]);
                            group.RotateZ.AddKey(frame, quat[2]);
                            group.RotateW.AddKey(frame, quat[3]);
                        }
                    }
                    break;
                case 0x15: //4 Singles Constant
                    {
                        float[] quat = reader.ReadSingles(4);
                        group.RotateX.AddKey(0, quat[0]);
                        group.RotateY.AddKey(0, quat[1]);
                        group.RotateZ.AddKey(0, quat[2]);
                        group.RotateW.AddKey(0, quat[3]);
                    }
                    break;
                case 0x16: //Short Quat Constant
                    {
                        //Todo this gives weird results.
                        short[] quat = reader.ReadInt16s(4);
                      /*  group.RotateX.AddKey(0, quat[0] / 0x7FFF);
                        group.RotateY.AddKey(0, quat[1] / 0x7FFF);
                        group.RotateZ.AddKey(0, quat[2] / 0x7FFF);
                        group.RotateW.AddKey(0, quat[3] / 0x7FFF);*/

                        Console.WriteLine($"track {track.OpCode} Unknown {track.Unknown} {string.Join(",", quat)}");
                    }
                    break;
                case 0x17: //Short X Axis Angle
                    {
                        var euler = new Vector3(reader.ReadInt16() / 180.0f, 0, 0) * STMath.Deg2Rad;
                        var quat = STMath.FromEulerAngles(euler);
                        group.RotateX.AddKey(0, quat.X);
                        group.RotateY.AddKey(0, quat.Y);
                        group.RotateZ.AddKey(0, quat.Z);
                        group.RotateW.AddKey(0, quat.W);
                    }
                    break;
                case 0x18: //Short Y Axis Angle (Degrees) Frame Count
                    {
                        for (int f = 0; f < header.FrameCount; f++)
                        {
                            float frame = f;

                            var euler = new Vector3(0, reader.ReadInt16() / 180.0f, 0) * STMath.Deg2Rad;
                            var quat = STMath.FromEulerAngles(euler);
                            group.RotateX.AddKey(frame, quat.X);
                            group.RotateY.AddKey(frame, quat.Y);
                            group.RotateZ.AddKey(frame, quat.Z);
                            group.RotateW.AddKey(frame, quat.W);
                        }
                    }
                    break;
                case 0x19: //Short Z Axis Angle (Degrees) Frame Count
                    {
                        for (int f = 0; f < header.FrameCount; f++)
                        {
                            float frame = f;

                            var euler = new Vector3(0, 0, reader.ReadInt16() / 180.0f) * STMath.Deg2Rad;
                            var quat = STMath.FromEulerAngles(euler);
                            group.RotateX.AddKey(frame, quat.X);
                            group.RotateY.AddKey(frame, quat.Y);
                            group.RotateZ.AddKey(frame, quat.Z);
                            group.RotateW.AddKey(frame, quat.W);
                        }
                    }
                    break;
                    //Todo these give weird results.
         /*       case 0x1A: //Consta
                    {
                        ushort flag = reader.ReadUInt16();
                        short[] angles = reader.ReadInt16s(2);
                        ushort[] param = reader.ReadUInt16s(2);

                        var euler = new Vector3(reader.ReadInt16() / 180.0f, 0, 0) * STMath.Deg2Rad;
                        var quat = STMath.FromEulerAngles(euler);
                        group.RotateX.AddKey(0, quat.X);
                        group.RotateY.AddKey(0, quat.Y);
                        group.RotateZ.AddKey(0, quat.Z);
                        group.RotateW.AddKey(0, quat.W);
                    }
                    break;
                case 0x1B:
                    {
                        ushort flag = reader.ReadUInt16();
                        //  short[] quat = reader.ReadInt16s(4);

                        var euler = new Vector3(0, reader.ReadInt16() / 180.0f, 0) * STMath.Deg2Rad;
                        var quat = STMath.FromEulerAngles(euler);
                        group.RotateX.AddKey(0, quat.X);
                        group.RotateY.AddKey(0, quat.Y);
                        group.RotateZ.AddKey(0, quat.Z);
                        group.RotateW.AddKey(0, quat.W);
                    }
                    break;
                case 0x1C:
                    {
                        ushort flag = reader.ReadUInt16();
                      //  short[] quat = reader.ReadInt16s(4);

                        var euler = new Vector3(0, 0, reader.ReadInt16() / 180.0f) * STMath.Deg2Rad;
                        var quat = STMath.FromEulerAngles(euler);
                        group.RotateX.AddKey(0, quat.X);
                        group.RotateY.AddKey(0, quat.Y);
                        group.RotateZ.AddKey(0, quat.Z);
                        group.RotateW.AddKey(0, quat.W);
                    }
                    break;*/
                default:
                    Console.WriteLine($"Unknown Op Code! Track {track.Index} Type {track.Type} OpCode {track.OpCode}");
                    break;
            }
        }
    }
}
