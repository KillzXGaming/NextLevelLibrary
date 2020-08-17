using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class TextureWrapper : ObjectTreeNode
    {
        private DATA_Parser.FileEntry File;
        private DICT.GameVersion Version;

        public TextureWrapper(DATA_Parser.FileEntry file, DATA_Parser dataParser)
        {
            Label = Hashing.CreateHashString(file.Hash);
            ImageKey = "Texture";

            File = file;
            Version = dataParser.Version;

            try
            {
                OnClick();
            }
            catch { }
        }

        private bool loaded = false;
        public override void OnClick()
        {
            if (loaded) return;

            if (Version == DICT.GameVersion.LM3)
                Tag = LM3.TextureChunk.Read(File);
            if (Version == DICT.GameVersion.LM2)
                Tag = LM2.TextureChunk.Read(File);

            loaded = true;
        }
    }
}
