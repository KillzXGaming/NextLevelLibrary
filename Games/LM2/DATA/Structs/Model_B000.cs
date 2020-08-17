using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;
using OpenTK;
using System.Security.Cryptography;
using System.Xml.Schema;

namespace NextLevelLibrary.LM2
{
    public class ModelChunk
    {
        const uint MODEL_HEADER_SIZE = 16;
        const uint MESH_SIZE = 0x28;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Hash;
            public uint MeshCount;
            public uint Unknown;
            public uint Padding;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Mesh
        {
            public uint IndexOffset;
            public ushort IndexCount;
            public ushort IndexFormat;
            public ushort BufferPointerOffset;
            public ushort Padding;
            public uint VertexFormatHash;
            public uint MaterialHash;
            public uint Unknown;
            public uint Unknown2;
            public uint MaterialLookupIndex; //Multiply by 4 to get offset in table
            public ushort VertexCount;
            public ushort Unknown4;
            public uint Hash;
        }

        public class ModelInfo
        {
            public uint Hash { get; set; }

            public Dictionary<uint, Matrix4> BoneData = new Dictionary<uint, Matrix4>();

            public Matrix4 Transform { get; set; } = Matrix4.Identity;

            public Stream MaterialData { get; set; }

            public List<MeshInfo> Meshes = new List<MeshInfo>();
        }

        public class MeshInfo
        {
            public Mesh MeshHeader { get; set; }
            public MaterialData Material { get; set; }

            public uint VertexBufferPointer { get; set; }
            public uint SkinningBufferPointer { get; set; }

            //  public bool HasSkinning => MeshHeader.SkinningFlags != 0xFFFFFF;
            public uint IndexCount => MeshHeader.IndexCount;
            public uint IndexType => MeshHeader.IndexFormat;

            public uint[] Faces { get; set; }

            public List<STVertex> Vertices = new List<STVertex>();

            public MeshInfo(Mesh mesh) { MeshHeader = mesh; }
        }

        public class MaterialData
        {
            public uint DiffuseTextureHash { get; set; }
            public uint ShadowTextureHash { get; set; }

            public bool IsAmbientMap { get; set; }
            public bool HasShadowMap { get; set; }
        }

        public static List<ModelInfo> Read(List<ChunkTable.ChunkDataEntry> chunkList,
            Dictionary<uint, int> HashToBoneID)
        {
            List<ModelInfo> models = new List<ModelInfo>();
            List<Header> modelHeaders = new List<Header>();
            List<MeshInfo> meshes = new List<MeshInfo>();
            Dictionary<uint, Matrix4> boneData = new Dictionary<uint, Matrix4>();
            List<Matrix4> modelMatrices = new List<Matrix4>();

            var vertexPointerChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.VertexStartPointers);

            var bufferChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.MeshBuffers);

            //Empty model, skip
            if (bufferChunk.Data.Length == 0)
                return models;

            for (int i = 0; i < chunkList.Count; i++)
            {
                var chunk = chunkList[i];
                switch (chunk.ChunkType)
                {
                    case ChunkDataType.ModelInfo:
                        uint numModels = (uint)chunk.Data.Length / MODEL_HEADER_SIZE;
                        modelHeaders = chunk.ReadStructs<Header>(numModels);
                        break;
                    case ChunkDataType.ModelTransform:
                        int numTransforms = (int)chunk.Data.Length / 64;
                        using (var reader = new FileReader(chunk.Data, true))
                        {
                            for (int j = 0; j < numTransforms; j++) {
                                var values = reader.ReadSingles(16);
                                modelMatrices.Add(new Matrix4(
                                    values[0], values[1], values[2], values[3],
                                    values[4], values[5], values[6], values[7],
                                    values[8], values[9], values[10], values[11],
                                    values[12], values[13], values[14], values[15]));
                            }
                        }
                        break;
                    case ChunkDataType.BoneData:
                        uint numBones = (uint)chunk.Data.Length / 68;
                        using (var reader = new FileReader(chunk.Data, true)) {
                            for (int m = 0; m < numBones; m++)
                            {
                                uint boneHash = reader.ReadUInt32();
                                var transform = reader.ReadSingles(16);
                                var boneTransform = new Matrix4(
                                    transform[0], transform[1], transform[2], transform[3],
                                    transform[4], transform[5], transform[6], transform[7],
                                    transform[8], transform[9], transform[10], transform[11],
                                    transform[12], transform[13], transform[14], transform[15]);
                                boneData.Add(boneHash, boneTransform);
                            }
                        }
                        break;
                    case ChunkDataType.MeshInfo:
                        uint numMeshes = (uint)chunk.Data.Length / MESH_SIZE;
                        var meshHeaders = chunk.ReadStructs<Mesh>(numMeshes);
                        //Load mesh structs into mesh info
                        //This will store vertex and face data

                        //Read vertex points. These must be parsed by mesh header data
                        using (var reader = new FileReader(vertexPointerChunk.Data, true))
                        {
                            for (int m = 0; m < numMeshes; m++)
                            {
                                var mesh = new MeshInfo(meshHeaders[m]);
                                meshes.Add(mesh);

                                if (reader.BaseStream.Length >= reader.Position + 4)
                                    mesh.VertexBufferPointer = reader.ReadUInt32();
                            }
                        }
                        break;
                }
            }

            int meshIndex = 0;
            for (int i = 0; i < modelHeaders.Count; i++)
            {
                var model = new ModelInfo() { Hash = modelHeaders[i].Hash };
                model.BoneData = boneData;
                models.Add(model);

                if (modelMatrices.Count > i)
                    model.Transform = modelMatrices[i];

                for (int j = 0; j < modelHeaders[i].MeshCount; j++)
                    model.Meshes.Add(meshes[meshIndex + j]);

                meshIndex += (int)modelHeaders[i].MeshCount;
            }

            var boneStart = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.BoneStart);

            var boneHashChunks = boneStart != null ? boneStart.SubData.Where(
                x => x.ChunkType == ChunkDataType.BoneHashList).ToList() : new List<ChunkTable.ChunkDataEntry>();

            var materialLookupChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.MaterialLookupTable).Data;
            var materialChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.MaterialData).Data;

            //Read materials
            MaterialLoaderHelper.ParseMaterials(materialChunk, materialLookupChunk, meshes);

            //Read vertex and face data
            ReadBufferData(bufferChunk.Data, HashToBoneID, boneHashChunks, models);

            return models;
        }

        static void ReadBufferData(Stream bufferChunk, Dictionary<uint, int> HashToBoneID,
            List<ChunkTable.ChunkDataEntry> boneHashChunks, List<ModelInfo> models)
        {
            int skinningIndex = 0;

            //Lastly parse the vertex and index buffers
            using (var reader = new FileReader(bufferChunk, true))
            {
                foreach (var model in models)
                {
                    for (int i = 0; i < model.Meshes.Count; i++) {
                        var mesh = model.Meshes[i];

                        //Get our bone hash index lists
                        Dictionary<int, uint> boneIDToHash = new Dictionary<int, uint>();

                        if (boneHashChunks.Count > skinningIndex)
                        {
                            var boneHashChunk = boneHashChunks[skinningIndex];
                            uint count = (uint)boneHashChunk.Data.Length / 4;
                            var hashes = boneHashChunk.ReadPrimitive<uint>(count);
                            for (int j = 0; j < count; j++)
                                boneIDToHash.Add(j, hashes[j]);
                        }

                        reader.SeekBegin(mesh.MeshHeader.IndexOffset);

                        mesh.Faces = new uint[mesh.IndexCount];
                        if (mesh.IndexType == 0x8000)
                        {
                            for (int f = 0; f < mesh.IndexCount; f++)
                                mesh.Faces[f] = reader.ReadByte();
                        }
                        else
                        {
                            for (int f = 0; f < mesh.IndexCount; f++)
                                mesh.Faces[f] = reader.ReadUInt16();
                        }

                        uint vertexStride = VertexLoaderExtension.GetStride(mesh.MeshHeader.VertexFormatHash);

                        for (int v = 0; v < mesh.MeshHeader.VertexCount; v++)
                        {
                            reader.SeekBegin(mesh.VertexBufferPointer + (v * vertexStride));
                            STVertex vertex = reader.ReadVertexLayout(mesh.MeshHeader.VertexFormatHash);

                            for (int j = 0; j < vertex.BoneIndices.Count; j++)
                            {
                                if (HashToBoneID.Count == 0)
                                    break;

                                uint boneHash = boneIDToHash[vertex.BoneIndices[j]];
                                //Get the index used from the skeleton's hash list
                                int boneIndex = HashToBoneID[boneHash];

                                vertex.BoneIndices[j] = boneIndex;
                            }


                            vertex.Position = Vector3.TransformPosition(vertex.Position, model.Transform);
                            //Transform 90 degrees and scale
                            vertex.Position = new Vector3(
                                vertex.Position.X,
                                vertex.Position.Z,
                                -vertex.Position.Y) * ModelWrapper.PreviewScale;
                            vertex.Normal = new Vector3(
                                vertex.Normal.X,
                                vertex.Normal.Z,
                                -vertex.Normal.Y);

                            mesh.Vertices.Add(vertex);
                        }

                        if (mesh.Vertices.Any(x => x.BoneIndices.Count > 0))
                            skinningIndex += 1;
                    }
                }
            }
        }
    }
}
