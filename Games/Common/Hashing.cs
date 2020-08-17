using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Toolbox.Core.IO;

namespace NextLevelLibrary
{
    public class Hashing
    {
        private static Dictionary<uint, string> hashNames = new Dictionary<uint, string>();

        public static Dictionary<uint, string> HashNames
        {
            get
            {
                if (hashNames?.Count == 0)
                    LoadHashes();

                return hashNames;
            }
        }

        public static void LoadHashes()
        {
            LoadHashes(Properties.Resources.FileNames);
            LoadHashes(Properties.Resources.BoneNames);
            LoadHashes(Properties.Resources.MaterialNames);
            LoadHashes(Properties.Resources.ScriptStrings);
            LoadHashes(Properties.Resources.Misc);

            for (int i = 0; i < 20; i++)
                LoadHash(string.Format("bip01_rarm_bone{0:00}_exp", i));
            for (int i = 0; i < 20; i++)
                LoadHash(string.Format("bip01_tail{0:00}_exp", i));
            for (int i = 0; i < 20; i++)
                LoadHash(string.Format("bip01_crown{0:00}_exp", i));
            for (int i = 0; i < 20; i++)
                LoadHash(string.Format("bip01_tongue{0:00}_exp", i));
            for (int i = 0; i < 40; i++)
                LoadHash(string.Format("bip01_larmdigit{0:00}_exp", i));
            for (int i = 0; i < 40; i++)
                LoadHash(string.Format("bip01_rarmdigit{0:00}_exp", i));
        }

        static void LoadHashes(string hashList)
        {
            foreach (string hashStr in hashList.Split('\n'))
            {
                string HashString = hashStr.TrimEnd();

                uint hash = StringToHash(HashString);
                uint lowerhash = StringToHash(HashString.ToLower());

                if (!hashNames.ContainsKey(hash))
                    hashNames.Add(hash, HashString);
                if (!hashNames.ContainsKey(lowerhash))
                    hashNames.Add(lowerhash, HashString.ToLower());

                string[] hashPaths = HashString.Split('/');
                for (int i = 0; i < hashPaths?.Length; i++)
                {
                    hash = StringToHash(hashPaths[i]);
                    if (!hashNames.ContainsKey(hash))
                        hashNames.Add(hash, HashString);
                }
            }
        }

        static void LoadHash(string HashString)
        {
            uint hash = StringToHash(HashString);
            uint lowerhash = StringToHash(HashString.ToLower());

            if (!hashNames.ContainsKey(hash))
                hashNames.Add(hash, HashString);
            if (!hashNames.ContainsKey(lowerhash))
                hashNames.Add(lowerhash, HashString.ToLower());
        }

        //From (Works as tested comparing hashbin strings/hashes
        //https://gist.github.com/RoadrunnerWMC/f4253ef38c8f51869674a46ee73eaa9f

        /// <summary>
        /// Calculates a string to a hash value used by NLG files.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="caseSensative"></param>
        /// <returns></returns>
        public static uint StringToHash(string name, bool caseSensative = false)
        {
            byte[] data = Encoding.Default.GetBytes(name);

            int h = -1;
            for (int i = 0; i < data.Length; i++)
            {
                int c = (int)data[i];
                if (caseSensative && ((c - 65) & 0xFFFFFFFF) <= 0x19)
                    c |= 0x20;

                h = (int)((h * 33 + c) & 0xFFFFFFFF);
            }

            return (uint)h;
        }

        public static string CreateHashString(uint hash)
        {
            if (HashNames.ContainsKey(hash)) return HashNames[hash];
            return hash.ToString();
        }

        public static void SearchHashMatch(Stream stream, int blockIndex)
        {
            if (stream == null) return;

            using (var reader = new FileReader(stream))
            {
                reader.SetByteOrder(false);
                while (reader.Position <= reader.BaseStream.Length - 4)
                {
                    uint hashCheck = reader.ReadUInt32();
                    if (HashNames.ContainsKey(hashCheck) && hashCheck > 0x50 && hashCheck != 0xFFFFFFFF)
                    {
                        Console.WriteLine($"HASH MATCH  blockIndex {blockIndex} {hashCheck.ToString("X")} {HashNames[hashCheck]} | {reader.Position}");
                    }
                    else
                        reader.Seek(-3);
                }
            }
        }
    }
}
