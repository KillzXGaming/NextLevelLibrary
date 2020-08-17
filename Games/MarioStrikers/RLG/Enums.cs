using System;
using System.Collections.Generic;
using System.Text;

namespace NextLevelLibrary.MarioStrikers
{
    public enum SectionMagic : uint
    {
        MaterialData = 0x0001B016,
        IndexData = 0x0001B007,
        VertexData = 0x0001B006,
        VertexAttributePointerData = 0x0001B005,
        MeshData = 0x0001B004,
        ModelData = 0x0001B003,
        MatrixData = 0x0001B002,
        SkeletonData = 0x8001B008,
        BoneHashes = 0x0001B00B,
        BoneData = 0x0001B00A,
        UnknownHashList = 0x0001B00C,
    }

    public enum MaterailPresets : uint
    {
        EnvDiffuseDamage = 0x1ACE1D01,
    }
}
