using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class ScriptWrapper : ObjectTreeNode
    {
        private DATA_Parser.FileEntry File;
        private DICT.GameVersion Version;

        public ScriptWrapper(DATA_Parser.FileEntry file, DATA_Parser dataParser)
        {
            Label = Hashing.CreateHashString(file.Hash);

            File = file;
            Version = dataParser.Version;

            OnClick();
        }

        private bool loaded = false;
        public override void OnClick()
        {
            if (loaded) return;

            if (Version == DICT.GameVersion.LM3)
                LM3.SciptChunk.Read(File.ChunkEntry.SubData);
            if (Version == DICT.GameVersion.LM2)
                LM2.SciptChunk.Read(File.ChunkEntry.SubData);

            loaded = true;
        }
    }
}
