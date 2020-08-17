using Collada141;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary.LM3
{
    public class MaterialLoaderHelper
    {
        /// <summary>
        /// Gets the total amount of pointers used for each mesh's material.
        /// </summary>
        /// <returns></returns>
        static List<uint[]> GetLookupPointers(List<ModelChunk.MeshInfo> meshes, Stream lookupStream)
        {
            List<uint[]> pointers = new List<uint[]>();
            using (var lookupReader = new FileReader(lookupStream, true))
            {
                for (int i = 0; i < meshes.Count; i++) {
                    byte numPointers = meshes[i].MeshHeader.MaterialLookupPointerCount;
                    pointers.Add(lookupReader.ReadUInt32s(numPointers));
                }
            }

            return pointers;
        }

        public static List<ObjectTreeNode> CreateMaterialChunkList(Stream materialStream, Stream lookupStream, List<ModelChunk.MeshInfo> meshes)
        {
            List<uint[]> materialPointers = GetLookupPointers(meshes, lookupStream);
            for (int i = 0; i < meshes.Count; i++) {
                materialPointers[i] = materialPointers[i].Where(x => x != 0 && x < materialStream.Length).ToArray();

                for (int j = 0; j < materialPointers[i].Length; j++)
                    Console.WriteLine($"pointer_{i} { materialPointers[i][j]}");
            }

            materialPointers = materialPointers.OrderBy(x => x[0]).ToList();

            List<ObjectTreeNode> meshNodes = new List<ObjectTreeNode>();
            using (var reader = new FileReader(materialStream, true))
            {
                for (int i = 0; i < meshes.Count; i++)
                {
                    ObjectTreeNode meshNode = new ObjectTreeNode();
                    meshNodes.Add(meshNode);

                    uint[] pointers = materialPointers[i];
                    for (int j = 0; j < pointers.Length; j++)
                    {
                        uint size = 0;
                        if (j + 1 < pointers.Length)
                            size = pointers[j + 1] - pointers[j];
                        else if (i + 1 < meshes.Count)
                            size = materialPointers[i + 1][0] - pointers[j];
                        else
                            size = (uint)materialStream.Length - pointers[j];

                        Console.WriteLine($"pointer {pointers[j]} size {size}");
                            
                        ObjectTreeNode matNode = new ObjectTreeNode($"Mat_{j}");
                        matNode.Tag = new SubStream(materialStream, pointers[j], size);
                        meshNode.AddChild(matNode);
                    }
                }
            }
            return meshNodes;
        }

        static int GetTextureSlotLookupIndex(string preset)
        {
            switch (preset)
            {
                case "base_metal_map":
                case "base_luigi":
                case "base_environment":
                case "base_skin":
                case "base_translucent":
                case "egadd_glasses_lens":
                case "kingboo_eyes2":
                case "ghost_vip_dj_hair":
                case "base_luigi_fabric":
                case "complex_emissive":
                case "golddark":
                case "treeshader_cutout_01":
                case "treeshader_base_01":
                case "base_environment_detailn":
                case "intromovie_terrain_01":
                    return 16;
                case "base_prop":
                case "baseghostmaterial":
                case "luigi_g_poltergust_body":
                    return 17;
                default:
                    return -1;
            }
        }

        static Dictionary<TEXTURE_SLOT, uint> GetTextureSlotsOffset(string preset)
        {
            Dictionary<TEXTURE_SLOT, uint> slots = new Dictionary<TEXTURE_SLOT, uint>();
            switch (preset)
            {
                case "base_metal_map":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.ROUGHNESS, 24);
                    slots.Add(TEXTURE_SLOT.METALNESS, 36);
                    break;
                case "base_prop":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.NORMAL, 12);
                    break;
                case "baseghostmaterial":
                    slots.Add(TEXTURE_SLOT.NORMAL, 0);
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 24);
                    slots.Add(TEXTURE_SLOT.MULTI, 60);
                    slots.Add(TEXTURE_SLOT.AMBIENT, 72);
                    break;
                case "base_environment":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.NORMAL, 12);
                    slots.Add(TEXTURE_SLOT.ROUGHNESS, 60);
                    slots.Add(TEXTURE_SLOT.METALNESS, 72);
                    break;
                case "base_environment_detailn":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.NORMAL, 12);
                    break;
                case "kingboo_eyes2":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.EMISSION, 12);
                    break;
                case "egadd_glasses_lens":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.METALNESS, 12); //Todo, unsure
                    slots.Add(TEXTURE_SLOT.NORMAL, 24);
                    slots.Add(TEXTURE_SLOT.ROUGHNESS, 36); //Todo, unsure
                    slots.Add(TEXTURE_SLOT.AMBIENT, 48);
                    break;
                case "ghost_vip_dj_hair":
                case "base_luigi":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.NORMAL, 12);
                    slots.Add(TEXTURE_SLOT.AMBIENT, 24);
                    break;
                case "base_luigi_fabric":
                    slots.Add(TEXTURE_SLOT.NORMAL, 0);
                    slots.Add(TEXTURE_SLOT.ROUGHNESS, 12);
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 24);
                    break;
                case "base_translucent":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.NORMAL, 12);
                    break;
                case "ghost_boo":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.ROUGHNESS, 24);
                    break;
                case "complex_emissive":
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    slots.Add(TEXTURE_SLOT.NORMAL, 12);
                    slots.Add(TEXTURE_SLOT.EMISSION, 36);
                    break;
                default:
                    slots.Add(TEXTURE_SLOT.DIFFUSE, 0);
                    break;
            }
            return slots;
        }

        public enum TEXTURE_SLOT
        {
            DIFFUSE,
            NORMAL,
            EMISSION,
            MULTI,
            AMBIENT,
            METALNESS,
            ROUGHNESS,
        }

        public static void ParseMaterials(Stream materialStream, Stream lookupStream, List<ModelChunk.MeshInfo> meshes)
        {
            SetMaterialsHacky(materialStream, lookupStream, meshes);
            foreach (var preset in MaterialPresetInfos)
            {
                Console.WriteLine($"preset {preset.Key} ptr {preset.Value.LookupTexPointer}");
                foreach (var slot in preset.Value.textureSlots)
                    Console.WriteLine($"slot {slot.Key} {slot.Value}");
            }

            List<uint[]> materialPointers = GetLookupPointers(meshes, lookupStream);

            //Get lookup pointers
            using (var reader = new FileReader(materialStream, true))
            {
                for (int i = 0; i < meshes.Count; i++) {
                    var mat = new ModelChunk.MaterialData();
                    meshes[i].Material = mat;

                    Console.WriteLine($"MESH {i}");

                    uint[] pointers = materialPointers[i];
                    uint hash = meshes[i].MeshHeader.MaterialHash;
                    string materialPreset = Hashing.CreateHashString(hash);

                    List<uint> matPointers = new List<uint>();
                    for (int j = 0; j < pointers.Length; j++)
                    {
                        if (pointers[j] != uint.MaxValue && pointers[j] != 0) {
                            matPointers.Add(pointers[j]);
                        }
                    }

                    var refList = pointers.ToList();
                    for (int j = 0; j < matPointers.Count; j++)
                    {
                        if (j + 1 < matPointers.Count)
                        {
                            uint size = matPointers[j + 1] - matPointers[j];
                            reader.SeekBegin(matPointers[j]);
                            while (reader.Position < matPointers[j] + size)
                            {
                                uint hashCheck = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(hashCheck)))
                                {
                                    if (mat.DiffuseHash == 0)
                                        mat.DiffuseHash = hashCheck;

                                    Console.WriteLine($"{materialPreset} TEXTURE {hashCheck} pointer {refList.IndexOf(matPointers[j])} position {reader.Position - 4 - matPointers[j]}");
                                }
                            }
                        }
                    }

                    int textureSlotIndex = GetTextureSlotLookupIndex(materialPreset);
                    Console.WriteLine($"textureSlotIndex");
                    if (textureSlotIndex != -1)
                    {
                        var textureSlots = GetTextureSlotsOffset(materialPreset);
                        if (textureSlots.ContainsKey(TEXTURE_SLOT.DIFFUSE)) {
                            reader.SeekBegin(pointers[textureSlotIndex] + textureSlots[TEXTURE_SLOT.DIFFUSE]);
                            mat.DiffuseHash = reader.ReadUInt32();
                        }
                        if (textureSlots.ContainsKey(TEXTURE_SLOT.NORMAL)) {
                            reader.SeekBegin(pointers[textureSlotIndex] + textureSlots[TEXTURE_SLOT.NORMAL]);
                            mat.NormalMapHash = reader.ReadUInt32();
                        }
                    }
                }
            }
        }

        static Dictionary<string, MaterialInfo> MaterialPresetInfos = new Dictionary<string, MaterialInfo>();

        public class MaterialInfo
        {
            public int LookupTexPointer = 0;
            public Dictionary<TEXTURE_SLOT, uint> textureSlots = new Dictionary<TEXTURE_SLOT, uint>();
        }

		//Thanks to Dimy for this method. It's nasty but it gets textures.
		//The game uses 100s of varied presets so we should try a work around for undefined presets.
        static void SetMaterialsHacky(Stream materialStream, Stream lookupStream, List<ModelChunk.MeshInfo> meshes)
        {
            List<uint[]> materialPointers = GetLookupPointers(meshes, lookupStream);

            List<uint> globalPointers = new List<uint>();
            foreach (var pointerList in materialPointers.OrderBy(x => x[0]))
            {
                for (int i = 0; i < pointerList.Length; i++)
                {
                    if (pointerList[i] != uint.MaxValue && pointerList[i] != 0)
                        globalPointers.Add(pointerList[i]);
                }
            }
            
            //Get lookup pointers
            using (var reader = new FileReader(materialStream, true))
            {
                for (int i = 0; i < meshes.Count; i++)
                {
                    var mat = new ModelChunk.MaterialData();
                    meshes[i].Material = mat;

                    uint[] pointers = materialPointers[i];
                    uint hash = meshes[i].MeshHeader.MaterialHash;
                    string materialPreset = Hashing.CreateHashString(hash);

                    List<uint> matPointers = new List<uint>();
                    for (int j = 0; j < pointers.Length; j++)
                    {
                        if (pointers[j] != uint.MaxValue) {
                            matPointers.Add(pointers[j]);
                            break;
                        }
                    }


                    var matInfo = new MaterialInfo();
                    if (!MaterialPresetInfos.ContainsKey(materialPreset))
                        MaterialPresetInfos.Add(materialPreset, matInfo);

                    var refList = pointers.ToList();
                    for (int j = 0; j < matPointers.Count; j++)
                    {
                        int pointerIndex = refList.IndexOf(matPointers[j]);

                        if (matPointers[j] % 448 == 0 || globalPointers.Contains(matPointers[j] + 448))
                        {
                            reader.SeekBegin(matPointers[j]);
                            reader.ReadBytes(12);
                            uint texhash = reader.ReadUInt32();

                            matInfo.LookupTexPointer = pointerIndex;
                            if (texhash == 0x81800000)
                            {
                                reader.Seek(-8);
                                texhash = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.DIFFUSE, (uint)reader.Position - matPointers[j] - 16);
                                    mat.DiffuseHash = texhash;
                                }
                                reader.ReadBytes(4);
                                uint texhash2 = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash2)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.NORMAL, (uint)reader.Position - matPointers[j] - 16);
                                    mat.NormalMapHash = texhash2;
                                }
                            }
                            else
                            {
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.NORMAL, (uint)reader.Position - matPointers[j] - 16);
                                    mat.NormalMapHash = texhash;
                                }

                                reader.ReadBytes(8);
                                reader.ReadBytes(0xC);
                                uint texhash2 = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash2)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.DIFFUSE, (uint)reader.Position - matPointers[j] - 16);
                                    mat.DiffuseHash = texhash2;
                                }
                            }
                        }
                        else if (matPointers[j] % 192 == 0 || globalPointers.Contains(matPointers[j] + 192))
                        {
                            reader.SeekBegin(matPointers[j]);
                            reader.ReadBytes(8);
                            uint texhash = reader.ReadUInt32();
                            uint a = reader.ReadUInt32();

                            Console.WriteLine($"{materialPreset} {pointerIndex}");
                            if (texhash == 0x81800000)
                            {
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash)))
                            
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.NORMAL, (uint)reader.Position - matPointers[j] - 12);
                                    mat.NormalMapHash = texhash;
                                }
                                reader.ReadBytes(4);
                                reader.ReadBytes(0xC);
                                uint texhash2 = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash2)))
                                {
                                    Console.WriteLine($"DIFFUSE {reader.Position - matPointers[j] - 12}");
                                    mat.DiffuseHash = texhash2;
                                }

                            }
                            else
                            {
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.DIFFUSE, (uint)reader.Position - matPointers[j] - 12);
                                    mat.DiffuseHash = texhash;
                                }
                                reader.ReadBytes(4);
                                uint texhash2 = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash2)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.NORMAL, (uint)reader.Position - matPointers[j] - 12);
                                    mat.NormalMapHash = texhash2;
                                }
                            }
                        }
                        else
                        {
                            reader.SeekBegin(matPointers[j]);
                            uint a = 0;
                            while (a != 0x81800000 && a != 0x01800000 && reader.Position < reader.BaseStream.Length - 4)
                            {
                                a = reader.ReadUInt32();
                            }
                            if (a == 0x81800000)
                            {
                                Console.WriteLine($"{materialPreset} {pointerIndex}");

                                reader.Seek(-8);
                                uint hash1 = reader.ReadUInt32();

                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(hash1)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.DIFFUSE, (uint)reader.Position - matPointers[j] - 4);
                                    mat.DiffuseHash = hash1;
                                }
                                reader.ReadBytes(8);
                                uint texhash2 = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash2)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.NORMAL, (uint)reader.Position - matPointers[j] - 4);
                                    mat.NormalMapHash = texhash2;
                                }
                            }
                            else if (a == 0x01800000)
                            {
                                reader.Seek(-8);
                                reader.ReadBytes(0x18);
                                uint hash1 = reader.ReadUInt32();

                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(hash1)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.DIFFUSE, (uint)reader.Position - matPointers[j] - 4);
                                    mat.DiffuseHash = hash1;
                                }
                                reader.ReadBytes(8);
                                uint texhash2 = reader.ReadUInt32();
                                if (Runtime.TextureCache.Any(x => x.Name == Hashing.CreateHashString(texhash2)))
                                {
                                    matInfo.textureSlots.Add(TEXTURE_SLOT.NORMAL, (uint)reader.Position - matPointers[j] - 4);
                                    mat.NormalMapHash = texhash2;
                                }
                            }
                        }
                    }

                    if (MaterialPresetInfos[materialPreset].textureSlots.Count < matInfo.textureSlots.Count)
                        MaterialPresetInfos[materialPreset] = matInfo;
                }
            }
        }
    }
}
