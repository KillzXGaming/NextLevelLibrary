using NextLevelLibrary.LM3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class SkeletonWrapper : ObjectTreeNode
    {
        private DATA_Parser.FileEntry File;
        private DATA_Parser DataParser;

        public SkeletonWrapper(DATA_Parser.FileEntry file, DATA_Parser dataParser)
        {
            Label = file.Hash.ToString();
            ImageKey = "Bone";

            File = file;
            DataParser = dataParser;
        }

        private bool loaded = false;
        public override void OnClick()
        {
            if (loaded) return;

            STGenericModel model = new STGenericModel(Label);
            model.Skeleton = new SkeletonFormat();
            model.Meshes.Add(new STGenericMesh());

            if (DataParser.Version == DICT.GameVersion.LM2)
                model.Skeleton = LM2.SkeletonChunk.Read(File.ChunkEntry.SubData);
            if (DataParser.Version == DICT.GameVersion.LM3)
                model.Skeleton = LM3.SkeletonChunk.Read(File.ChunkEntry.SubData);

            Tag = new ModelFormat(model);

            foreach (var child in model.CreateTreeHiearchy().Children)
                AddChild(child);

            loaded = true;
        }
    }
}
