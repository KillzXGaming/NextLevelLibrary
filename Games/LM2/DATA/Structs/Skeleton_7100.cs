using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Toolbox.Core;
using Toolbox.Core.IO;
using OpenTK;

namespace NextLevelLibrary.LM2
{
    public class SkeletonChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint BoneCount;
            public uint Padding1;
            public uint Padding2;
            public uint Padding3;
            public uint BoneIndexListCount;
            public uint Padding4;
            public uint Padding5;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BoneInfo
        {
            public uint Hash;
            public short ParentIndex;
            public short TotalChildCount;
            public ushort BoneIndex;
            public byte ChildCount;
            public byte Unknown3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BoneTransform
        {
            public float QuaternionX;
            public float QuaternionY;
            public float QuaternionZ;
            public float QuaternionW = 1;

            public float TranslationX;
            public float TranslationY;
            public float TranslationZ;

            public float ScaleX = 1;
            public float ScaleY = 1;
            public float ScaleZ = 1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BoneHash
        {
            public uint Hash;
            public uint Index;
        }

        public static SkeletonFormat Read(List<ChunkTable.ChunkDataEntry> chunkList)
        {
            var genericSkeleton = new SkeletonFormat();

            Header header = null;
            List<BoneInfo> boneInfos = new List<BoneInfo>();
            List<BoneTransform> boneTransforms = new List<BoneTransform>();
            List<uint> boneIndices = new List<uint>();
            List<BoneHash> boneHashes = new List<BoneHash>();

            for (int i = 0; i < chunkList.Count; i++)
            {
                if (chunkList[i].ChunkType == ChunkDataType.SkeletonHeader)
                    header = chunkList[i].ReadStruct<Header>();
            }

            for (int i = 0; i < chunkList.Count; i++)
            {
                var chunk = chunkList[i];
                Console.WriteLine($"SKELETON {chunk.ChunkType}");
                switch (chunk.ChunkType)
                {
                    case ChunkDataType.SkeletonBoneInfo:
                        boneInfos = chunk.ReadStructs<BoneInfo>(header.BoneCount);
                        break;
                    case ChunkDataType.SkeletonBoneTransform:
                        boneTransforms = chunk.ReadStructs<BoneTransform>(header.BoneCount);
                        break;
                    case ChunkDataType.SkeletonBoneIndexList:
                      //  boneIndices = chunk.ReadPrimitive<uint>(header.BoneIndexListCount);
                        break;
                    case ChunkDataType.SkeletonBoneHashList:
                        boneHashes = chunk.ReadStructs<BoneHash>(header.BoneCount);
                        break;
                }
            }

            for (int i = 0; i < boneInfos.Count; i++)
            {
                var info = boneInfos[i];
                var transform = boneTransforms[i];
                string name = Hashing.HashNames.ContainsKey(info.Hash) ?
                    Hashing.HashNames[info.Hash] : info.Hash.ToString();

                if (transform == null) transform = new BoneTransform();

                genericSkeleton.Bones.Add(new STBone(genericSkeleton)
                {
                    Name = name,
                    ParentIndex = info.ParentIndex,
                    Position = (new OpenTK.Vector3(
                        transform.TranslationX,
                        transform.TranslationY,
                        transform.TranslationZ) * (info.ParentIndex != -1 ? (32 * ModelWrapper.PreviewScale) : 1)),
                    Rotation = new OpenTK.Quaternion(
                              transform.QuaternionX,
                             transform.QuaternionY,
                             transform.QuaternionZ,
                             transform.QuaternionW) *
                             (info.ParentIndex == -1 ?
                             Quaternion.FromEulerAngles(-1.5708F, 0, 0) :
                             Quaternion.Identity),
                });

                Console.WriteLine($"BONE {transform.ScaleX} {transform.ScaleY} {transform.ScaleZ}");
            }

            for (int i = 0; i < boneHashes.Count; i++) {
                genericSkeleton.BoneHashToID.Add(boneHashes[i].Hash, (int)boneHashes[i].Index);
                Console.WriteLine($"BONEINDEXLIST {boneHashes[i].Hash} {boneHashes[i].Index}");
            }

            genericSkeleton.Reset();

            return genericSkeleton;
        }
    }
}
