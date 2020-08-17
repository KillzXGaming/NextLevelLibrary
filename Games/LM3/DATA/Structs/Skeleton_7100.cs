using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Toolbox.Core.IO;
using Toolbox.Core;
using OpenTK;

namespace NextLevelLibrary.LM3
{
    public class SkeletonChunk
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Padding1;
            public uint Padding2;
            public uint Padding3;
            public uint Padding4;
            public uint Padding5;
            public uint BoneCount;
            public uint BoneIndexListCount;
            public uint Padding6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BoneInfo
        {
            public uint Hash;
            public short TotalChildCount;
            public byte ChildCount;
            public byte Unknown3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BoneTransform
        {
            public float QuaternionX;
            public float QuaternionY;
            public float QuaternionZ;
            public float QuaternionW;

            public float TranslationX;
            public float TranslationY;
            public float TranslationZ;
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
            List<short> parentIndices = new List<short>();

            for (int i = 0; i < chunkList.Count; i++) {
                var chunk = chunkList[i];
                switch (chunk.ChunkType)
                {
                    case ChunkDataType.SkeletonHeader:
                        header = chunk.ReadStruct<Header>();
                        break;
                    case ChunkDataType.SkeletonBoneInfo:
                        boneInfos = chunk.ReadStructs<BoneInfo>(header.BoneCount);
                        break;
                    case ChunkDataType.SkeletonBoneTransform:
                        boneTransforms = chunk.ReadStructs<BoneTransform>(header.BoneCount);
                        break;
                    case ChunkDataType.SkeletonBoneIndexList:
                        boneIndices = chunk.ReadPrimitive<uint>(header.BoneIndexListCount);
                        break;
                    case ChunkDataType.SkeletonBoneHashList:
                        boneHashes = chunk.ReadStructs<BoneHash>(header.BoneCount);
                        break;
                    case ChunkDataType.SkeletonBoneParenting:
                        parentIndices = chunk.ReadPrimitive<short>(header.BoneCount);
                        break;
                }
            }

            for (int i = 0; i < boneInfos.Count; i++) {
                var info = boneInfos[i];
                var transform = boneTransforms[i];
                string name = Hashing.HashNames.ContainsKey(info.Hash) ?
                    Hashing.HashNames[info.Hash] : info.Hash.ToString();

                genericSkeleton.Bones.Add(new STBone(genericSkeleton)
                {
                    Name = name,
                    ParentIndex = parentIndices[i],
                    Position = new OpenTK.Vector3(
                        transform.TranslationX,
                        transform.TranslationY,
                        transform.TranslationZ) * ModelWrapper.PreviewScale,
                    Rotation = new OpenTK.Quaternion(
                        transform.QuaternionX,
                        transform.QuaternionY,
                        transform.QuaternionZ,
                        transform.QuaternionW) * 
                        (parentIndices[i] == -1 ? 
                        Quaternion.FromEulerAngles(-1.5708F, 0, -1.5708F) :
                        Quaternion.Identity),
                });
            }

            for (int i = 0; i < boneHashes.Count; i++) {
                genericSkeleton.BoneHashToID.Add(boneHashes[i].Hash, (int)boneHashes[i].Index);

            }
            genericSkeleton.Reset();

            return genericSkeleton;
        }
    }
}
