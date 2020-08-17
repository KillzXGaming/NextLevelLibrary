using NextLevelLibrary.LM3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class ModelWrapper : ObjectTreeNode
    {
        public static float PreviewScale { get; set; } = 4.0f;

        private DATA_Parser.FileEntry File;
        private DATA_Parser DataParser;

        public ModelWrapper(DATA_Parser.FileEntry file, DATA_Parser dataParser)
        {
            Label = file.Hash.ToString();
            ImageKey = "Model";

            File = file;
            DataParser = dataParser;
        }

        private bool loaded = false;
        public override void OnClick()
        {
            if (loaded) return;

            STGenericModel model = new STGenericModel(Label);
            model.Skeleton = new SkeletonFormat();
            for (int i = 0; i < DataParser.Files.Count; i++)
            {
                if (DataParser.Files[i].Hash == File.Hash &&
                    DataParser.Files[i].ChunkEntry.ChunkType == ChunkFileType.Skeleton)
                {
                    if (DataParser.Version == DICT.GameVersion.LM2)
                        model.Skeleton = LM2.SkeletonChunk.Read(DataParser.Files[i].ChunkEntry.SubData);
                    if (DataParser.Version == DICT.GameVersion.LM3)
                        model.Skeleton = LM3.SkeletonChunk.Read(DataParser.Files[i].ChunkEntry.SubData);
                }
            }

            Dictionary<uint, int> boneHashToID = ((SkeletonFormat)model.Skeleton).BoneHashToID;
            if (DataParser.Version == DICT.GameVersion.LM3)
            {
                var modelList = ModelChunk.Read(File.ChunkEntry.SubData, boneHashToID);

             /*   var materialChunk = File.ChunkEntry.SubData.FirstOrDefault(x => x.ChunkType == ChunkDataType.MaterialData);
                var materialLookupChunk = File.ChunkEntry.SubData.FirstOrDefault(x => x.ChunkType == ChunkDataType.MaterialLookupTable);

                var matChunks = MaterialLoaderHelper.CreateMaterialChunkList(materialChunk.Data, 
                    materialLookupChunk.Data, modelList.SelectMany(x => x.Meshes).ToList());*/

                int index = 0;
                foreach (var mdl in modelList)
                {
                    foreach (var mesh in mdl.Meshes)
                    {
                        var genericMesh = new STGenericMesh();
                        genericMesh.Name = Hashing.CreateHashString(mesh.MeshHeader.Hash);
                        if (Hashing.HashNames.ContainsKey(mesh.MeshHeader.MaterialHash))
                            genericMesh.Name += $"_{Hashing.CreateHashString(mesh.MeshHeader.MaterialHash)}";

                        Console.WriteLine($"MESH_HASHM {Hashing.CreateHashString(mesh.MeshHeader.MaterialHash)}");
                        Console.WriteLine($"MESH_HASHV {Hashing.CreateHashString(mesh.MeshHeader.VertexFormatHash)}");

                        genericMesh.Vertices.AddRange(mesh.Vertices);

                        var poly = new STPolygonGroup();
                        poly.Faces = mesh.Faces.ToList();
                        genericMesh.PolygonGroups.Add(poly);
                        model.Meshes.Add(genericMesh);

                        var material = new LMMaterial();
                        poly.Material = material;

                        material.TextureMaps.Add(new STGenericTextureMap()
                        {
                            Name = Hashing.CreateHashString(mesh.Material.DiffuseHash),
                            Type = STTextureType.Diffuse,
                        });
                        index++;
                    }
                }
            }
            else
            {
                var modelList = LM2.ModelChunk.Read(File.ChunkEntry.SubData, boneHashToID);
                foreach (var mdl in modelList)
                {
                    foreach (var mesh in mdl.Meshes)
                    {
                        var genericMesh = new STGenericMesh();
                        genericMesh.Name = Hashing.CreateHashString(mesh.MeshHeader.Hash);
                        if (Hashing.HashNames.ContainsKey(mesh.MeshHeader.MaterialHash))
                            genericMesh.Name += $"_{Hashing.CreateHashString(mesh.MeshHeader.MaterialHash)}";

                        genericMesh.Name += $"_{mesh.MeshHeader.VertexFormatHash}_{VertexLoaderExtension.GetStride(mesh.MeshHeader.VertexFormatHash)}";

                        uint vertexStride = VertexLoaderExtension.GetStride(mesh.MeshHeader.VertexFormatHash);

                        if (mesh.Vertices.Count == 0)
                            continue;

                        genericMesh.Vertices.AddRange(mesh.Vertices);

                        var poly = new STPolygonGroup();
                        poly.Faces = mesh.Faces.ToList();
                        genericMesh.PolygonGroups.Add(poly);
                        model.Meshes.Add(genericMesh);

                        var material = new LMMaterial();
                        material.IsAmbientMap = mesh.Material.IsAmbientMap;
                        poly.Material = material;

                        material.TextureMaps.Add(new STGenericTextureMap() {
                            Name = Hashing.CreateHashString(mesh.Material.DiffuseTextureHash),
                            Type = STTextureType.Diffuse,
                        });

                        if (mesh.Material.HasShadowMap)
                        {
                            material.TextureMaps.Add(new STGenericTextureMap()
                            {
                                Name = Hashing.CreateHashString(mesh.Material.ShadowTextureHash),
                                Type = STTextureType.Shadow,
                            });
                        }
                    }
                }
            }


            Tag = new ModelFormat(model);

            foreach (var child in model.CreateTreeHiearchy().Children)
                AddChild(child);

            loaded = true;
        }
    }
}
