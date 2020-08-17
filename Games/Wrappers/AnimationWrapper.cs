using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class AnimationWrapper : ObjectTreeNode
    {
        private DATA_Parser.FileEntry File;
        private DICT.GameVersion Version;

        public AnimationWrapper(DATA_Parser.FileEntry file, DATA_Parser dataParser)
        {
            Label = file.Hash.ToString();

            File = file;
            Version = dataParser.Version;

            AddChild(new ObjectTreeNode("RAW DATA") { Tag = file.ChunkEntry.Data });
        }

        public override ToolMenuItem[] GetContextMenuItems()
        {
            List<ToolMenuItem> menuItems = new List<ToolMenuItem>();
            menuItems.Add(new ToolMenuItem("Export Raw Data", ExportRawData));
            return menuItems.ToArray();
        }

        private void ExportRawData(object sender, EventArgs e)
        {
            string filePath = this.LoadFileDialog(Label);
            if (filePath != string.Empty) {
                File.ChunkEntry.Data.SaveToFile(filePath);
            }
        }

        private bool loaded = false;
        public override void OnClick()
        {
            if (loaded) return;

            if (Version == DICT.GameVersion.LM3)
                Tag = LM3.AnimationChunk.Read(File.ChunkEntry.Data);
            if (Version == DICT.GameVersion.LM2)
                Tag = LM2.AnimationChunk.Read(File.ChunkEntry.Data);

            loaded = true;
        }
    }
}
