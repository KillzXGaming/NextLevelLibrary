using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace NextLevelLibrary.LM2
{
    public class MaterialLoaderHelper
    {
        /// <summary>
        /// Gets the total amount of pointers used for each mesh's material.
        /// </summary>
        /// <returns></returns>
        static List<uint[]> GetLookupPointers(List<ModelChunk.MeshInfo> meshes, Stream lookupStream)
        {
            //Grab all the lookup indices
            List<uint> indices = meshes.Select(x => x.MeshHeader.MaterialLookupIndex).ToList();

            //Create an array of pointer counts
            uint[] lookupSizes = new uint[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                if (i + 1 < indices.Count)
                    lookupSizes[i] = indices[i + 1] - indices[i];
                else
                    lookupSizes[i] = (uint)(lookupStream.Length / 4) - indices[i];
            }

            List<uint[]> pointers = new List<uint[]>();
            using (var lookupReader = new FileReader(lookupStream, true))
            {
                for (int i = 0; i < meshes.Count; i++) {
                    lookupReader.SeekBegin(meshes[i].MeshHeader.MaterialLookupIndex * 4);
                    pointers.Add(lookupReader.ReadUInt32s((int)lookupSizes[i]));
                }
            }

            return pointers;
        }

        public static void ParseMaterials(Stream materialStream, Stream lookupStream, List<ModelChunk.MeshInfo> meshes)
        {
            List<uint[]> materialPointers = GetLookupPointers(meshes, lookupStream);

            //Get lookup pointers
            using (var materialReader = new FileReader(materialStream, true))
            {
                for (int i = 0; i < meshes.Count; i++) {
                    var mat = new ModelChunk.MaterialData();
                    string materialPreset = Hashing.CreateHashString(meshes[i].MeshHeader.MaterialHash);
                    uint[] pointers = materialPointers[i];

                    //Get pointer indices based on the preset
                    int texturePointerIndex = GetTextureSlotLookupIndex(materialPreset);
                    if (texturePointerIndex != -1) {
                        //Set slot parameters
                        mat.HasShadowMap = HasShadowMap(materialPreset);
                        mat.IsAmbientMap = HasSpecularMap(materialPreset);

                        materialReader.SeekBegin(pointers[texturePointerIndex]);
                        if (materialPreset == "pestmaterial/default")
                            materialReader.ReadUInt32();

                        mat.DiffuseTextureHash = materialReader.ReadUInt32();
                        if (mat.HasShadowMap)
                            mat.ShadowTextureHash = materialReader.ReadUInt32();
                    }
                    else
                    {
                        uint hash = SearchTextureLookups(materialReader, pointers);
                        if (hash != 0) {
                            mat.DiffuseTextureHash = hash;
                        }
                    }
                    meshes[i].Material = mat;
                }
            }
        }

        //Searchs all points to find valid texture pointers
        static uint SearchTextureLookups(FileReader materialReader, uint[] pointers)
        {
            for (int i = 0; i < pointers.Length; i++) {
                if (pointers[i] != uint.MaxValue && pointers[i] != 0) {
                    materialReader.SeekBegin(pointers[i]);
                    uint hash = materialReader.ReadUInt32();
                    if (Toolbox.Core.Runtime.TextureCache.Any(x => x.Name == hash.ToString()))
                    {
                       // Console.WriteLine($"TextureLookupIndex {i}");
                        return hash;
                    }
                }
            }
            return 0;
        }


        static bool HasSpecularMap(string preset)
        {
            switch (preset)
            {
                case "environmentspecularmaterial/default":
                case "environmentspecularrigidskin/default":
                    return true;
                default:
                    return false;
            }
        }

        static bool HasShadowMap(string preset)
        {
            switch (preset)
            {
                case "environmentmaterial/default":
                    return true;
                default:
                    return false;
            }
        }

        static int GetTextureSlotLookupIndex(string preset)
        {
            switch (preset)
            {
                case "morphluigimaterial/default":
                    return 11;
                case "windowmaterial/default":
                    return 9;
                case "luigieyematerial/default":
                case "luigimaterial/default":
                    return 7;
                case "environmentmaterial/default":
                case "environmentspecularmaterial/default":
                case "morphghostmaterial/default":
                case "pestmaterial/default":
                case "ghostmaterial/default":
                    return 6;
                case "environmentspecularrigidskin/default":
                    return 7;
                case "uvslidingmaterial/default":
                    return 4;
                case "uvslidingmaterialgs/default":
                case "diffuseskin/depthshell":
                    return 3;
                case "diffusevertcolor/default":
                    return 2;
                case "diffusevertcolor":
                default:    
                    return -1;
            }
        }
    }
}
