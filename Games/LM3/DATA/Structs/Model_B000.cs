using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;
using OpenTK;

namespace NextLevelLibrary.LM3
{
    public class ModelChunk
    {
        const uint MODEL_HEADER_SIZE = 12;
        const uint MESH_SIZE = 0x40;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Hash;
            public uint Unknown;
            public ushort MeshCount;
            public ushort Unknown2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Mesh
        {
            public uint Hash;
            public uint IndexOffset;
            public uint IndexFlags;
            public uint VertexCount;
            public byte VertexFlags;
            public byte MaterialLookupPointerCount;
            public ushort Padding;
            public ushort Unknown2;
            public ushort Unknown3;
            public uint MaterialHash;
            public uint VertexFormatHash;
            public uint Unknown4;
            public uint SkinningFlags; //0xFFFF for static meshes
            public ushort Unknown5;
            public ushort Unknown6;

            public uint Unknown7;
            public uint Unknown8;
            public uint Unknown9;
            public uint Unknown10;
            public uint Unknown11;
        }

        public class ModelInfo
        {
            public uint Hash { get; set; }

            public Matrix4 Transform { get; set; } = Matrix4.Identity;

            public Stream MaterialData { get; set; }

            public List<MeshInfo> Meshes = new List<MeshInfo>();
        }

        public class MeshInfo
        {
            public Mesh MeshHeader { get; set; }

            public uint VertexBufferPointer { get; set; }
            public uint SkinningBufferPointer { get; set; }

            public bool HasSkinning => MeshHeader.SkinningFlags != 0xFFFFFF;
            public uint IndexCount => MeshHeader.IndexFlags & 0xFFFFFF;
            public uint IndexType => MeshHeader.IndexFlags >> 24;

            public uint[] Faces { get; set; }

            public List<STVertex> Vertices = new List<STVertex>();

            public MaterialData Material { get; set; }

            public MeshInfo(Mesh mesh) { MeshHeader = mesh; }
        }

        public class MaterialData
        {
            public uint DiffuseHash { get; set; }
            public uint NormalMapHash { get; set; }
            public uint RoughnessMapHash { get; set; }
            public uint AmbientMapHash { get; set; }
        }

        public static List<ModelInfo> Read(List<ChunkTable.ChunkDataEntry> chunkList,
            Dictionary<uint, int> HashToBoneID)
        {
            List<ModelInfo> models = new List<ModelInfo>();
            List<Header> modelHeaders = new List<Header>();
            List<MeshInfo> meshes = new List<MeshInfo>();
            List<Matrix4> modelMatrices = new List<Matrix4>();

            var vertexPointerChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.VertexStartPointers);

            var bufferChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.MeshBuffers);

            for (int i = 0; i < chunkList.Count; i++) {
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

                                if (mesh.HasSkinning && reader.BaseStream.Length >= reader.Position + 4)
                                    mesh.SkinningBufferPointer = reader.ReadUInt32();
                                if (reader.BaseStream.Length >= reader.Position + 4)
                                    mesh.VertexBufferPointer = reader.ReadUInt32();
                                if (reader.BaseStream.Length >= reader.Position + 4)
                                    reader.ReadUInt32();
                                if (reader.BaseStream.Length >= reader.Position + 4)
                                    reader.ReadUInt32();
                            }
                        }
                        break;
                }
            }

            var materialChunk = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.MaterialData);

            var materialLookupChunk = chunkList.FirstOrDefault(
               x => x.ChunkType == ChunkDataType.MaterialLookupTable);

            MaterialLoaderHelper.ParseMaterials(materialChunk.Data, materialLookupChunk.Data, meshes);

            int meshIndex = 0;
            for (int i = 0; i < modelHeaders.Count; i++) {
                var model = new ModelInfo() { Hash = modelHeaders[i].Hash };
                models.Add(model);

                if (modelMatrices.Count > i)
                    model.Transform = modelMatrices[i];

                for (int j = 0; j < modelHeaders[i].MeshCount; j++)
                    model.Meshes.Add(meshes[meshIndex + j]);

                meshIndex += (int)modelHeaders[i].MeshCount;
            }

            //Get our bone hash index lists
            Dictionary<int, uint> boneIDToHash = new Dictionary<int, uint>();

            var boneStart = chunkList.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.BoneStart);

            var boneHashChunk = boneStart != null ? boneStart.SubData.FirstOrDefault(
                x => x.ChunkType == ChunkDataType.BoneHashList) : null;

            if (boneHashChunk != null)
            {
                uint count = (uint)boneHashChunk.Data.Length / 4;
                var hashes = boneHashChunk.ReadPrimitive<uint>(count);
                for (int i = 0; i < count; i++)
                    boneIDToHash.Add(i, hashes[i]);
            }

            //Lastly parse the vertex and index buffers
            using (var reader = new FileReader(bufferChunk.Data, true)) {
                foreach (var model in models)
                {
                    foreach (var mesh in model.Meshes) {
                        reader.SeekBegin(mesh.MeshHeader.IndexOffset);

                        mesh.Faces = new uint[mesh.IndexCount];
                        if (mesh.IndexType == 0x80)
                        {
                            for (int f = 0; f < mesh.IndexCount; f++)
                                mesh.Faces[f] = reader.ReadByte();
                        }
                        else if (mesh.IndexType == 0x40)
                        {
                            for (int f = 0; f < mesh.IndexCount; f++)
                                mesh.Faces[f] = reader.ReadUInt32();
                        }
                        else
                        {
                            for (int f = 0; f < mesh.IndexCount; f++)
                                mesh.Faces[f] = reader.ReadUInt16();
                        }

                        reader.SeekBegin(mesh.VertexBufferPointer);
                        for (int v = 0; v < mesh.MeshHeader.VertexCount; v++)
                        {
                            STVertex vertex = new STVertex();
                            vertex.Position = reader.ReadVec3();
                            float texCoordU = reader.ReadSingle();
                            vertex.Normal = reader.ReadVec3();
                            float texCoordV = reader.ReadSingle();
                            vertex.Tangent = reader.ReadVec4();

                            vertex.TexCoords = new Vector2[1]
                            {
                            new Vector2(texCoordU, texCoordV)
                            };

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

                        if (mesh.HasSkinning && HashToBoneID.Count > 0)
                        {
                            reader.SeekBegin(mesh.SkinningBufferPointer);
                            for (int v = 0; v < mesh.MeshHeader.VertexCount; v++)
                            {
                                byte[] boneIndices = reader.ReadBytes(4);
                                float[] weights = reader.ReadSingles(4);

                                for (int j = 0; j < 4; j++)
                                {
                                    if (weights[j] == 0)
                                        break;

                                    //Get the hash indexing the model's bone hash list
                                    uint boneHash = boneIDToHash[boneIndices[j]];
                                    //Get the index used from the skeleton's hash list
                                    int boneIndex = HashToBoneID[boneHash];

                                    mesh.Vertices[v].BoneIndices.Add(boneIndex);
                                    mesh.Vertices[v].BoneWeights.Add(weights[j]);
                                }
                            }
                        }
                    }
                }
            }
            return models;
        }
    }
}
