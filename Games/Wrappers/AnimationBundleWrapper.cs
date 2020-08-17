using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class AnimationBundleWrapper : ObjectTreeNode
    {
        private DATA_Parser.FileEntry File;
        private DICT.GameVersion Version;

        public AnimationBundleWrapper(DATA_Parser.FileEntry file, DATA_Parser dataParser,
            Dictionary<uint, DATA_Parser.FileEntry> animationFiles)
        {
            Label = file.Hash.ToString();
            Tag = file.ChunkEntry.Data;

            File = file;
            Version = dataParser.Version;

            using (var reader = new FileReader(file.ChunkEntry.Data, true))
            {
                uint numAnimations = reader.ReadUInt32();
                reader.ReadUInt32();
                for (int i = 0; i < numAnimations; i++)
                {
                    reader.ReadUInt32();
                    uint hash = reader.ReadUInt32();
                    if (animationFiles.ContainsKey(hash)) {

                        var fileNode = new AnimationWrapper(animationFiles[hash], dataParser);
                        AddChild(fileNode);
                    }
                }
            }
        }

        public override ToolMenuItem[] GetContextMenuItems()
        {
            List<ToolMenuItem> menuItems = new List<ToolMenuItem>();
            //    menuItems.Add(new ToolMenuItem("Export All", ExportRawData));
            return menuItems.ToArray();
        }
    }
}
