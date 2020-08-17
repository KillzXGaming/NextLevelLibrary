using System;
using System.Collections.Generic;
using System.Text;

namespace NextLevelLibrary
{
    public enum VertexDataFormat
    {
        Float16,
        Float32,
        Float32_32,
        Float32_32_32,
    }

    public enum ResourceType : byte
    {
        NONE = 0,
        TABLE = 1,
        DATA = 2,
    }

    public enum IndexFormat : ushort
    {
        Index_16 = 0x0,
        Index_8 = 0x8000,
    }

    public enum FileIdentifiers : uint
    {
        ModelFile = 0x30DB9D05,
        TextureFile = 0x50D377E9,
        AnimationFile = 0x24FA2400,
    }

    public enum BlockType : byte
    {
        FileTable = 0x4,
        FileData = 0x8,
    }

    public enum ChunkFileType : ushort
    {
        FileTable = 0x1,
        TextureBundles = 0x20,
        ModelBundles = 0x21,
        AnimationBundles = 0x1302,
        CutsceneNLB = 0x30,
        Config = 0x31, //Text based parameters
        Video = 0x1200, //MPEG Video File
        AudioBanks = 0x3000,
        Effects = 0x4000,
        Script = 0x5000,
        ScriptTable = 0x6500,
        MaterialEffects = 0xB300,
        Texture = 0xB500,
        Shaders = 0xB400,
        Model = 0xB000,
        Physics = 0xC300,
        ClothPhysics = 0xE000,
        CollisionParams = 0xD000,
        AnimationData = 0x7000,
        Font = 0x7010,
        MessageData = 0x7020,
        Skeleton = 0x7100,
        VNAND = 0x9501,
    }

    public enum ChunkDataType : ushort
    {
        CutsceneNLB = 0x1200,

        AudioData1 = 0xA251,
        AudioData2 = 0xA252,
        AudioData3 = 0xA253,
        AudioData4 = 0xA254,

        MaterialName = 0xB333,

        VertexShader = 0xB401,
        FragmentShader = 0xB402,
        GeometryShader = 0xB403,
        ComputeShader = 0xB404,

        TextureHeader = 0xB501,
        TextureData = 0xB502,

        MaterialData = 0xB006,
        MeshInfo = 0xB003,
        VertexStartPointers = 0xB004,
        ModelTransform = 0xB001, //Matrix4x4.
        ModelInfo = 0xB002, //Contains mesh count and model hash
        MeshBuffers = 0xB005, //Vertex and index buffer
        MaterialLookupTable = 0xB007,
        BoundingRadius = 0xB008,
        BoundingBox = 0xB009,

        FontData = 0x7011,
        MessageData = 0x7020,
        ShaderData = 0xB400,
        UILayoutStart = 0x7000,
        UILayoutHeader = 0x7001,
        UILayoutData = 0x7002, //Without header
        UILayout = 0x7003, //All parts combined

        HavokPhysics = 0xC900,
        PhysicData2 = 0xC901,
        CollisionPhysics = 0xC301, //Seems to be related to collisin for objects

        BoneStart = 0xB100,
        BoneData = 0xB102,
        BoneHashList = 0xB103,

        //Scripts handle various things.
        //Lighting, NIS cutscene triggers, object placements.
        ScriptData = 0x5012, 
        ScriptInfo = 0x5013,
        ScriptTable = 0x5014,

        ScriptHashTable = 0x6501,
        ScriptFileHash = 0x6503,

        SkeletonHeader = 0x7101,
        SkeletonBoneInfo = 0x7102,
        SkeletonBoneTransform = 0x7103,
        SkeletonBoneIndexList = 0x7104,
        SkeletonBoneHashList = 0x7105,
        SkeletonBoneParenting = 0x7106,
    }
}
