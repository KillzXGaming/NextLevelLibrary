using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace NextLevelLibrary.MarioStrikers
{
    public class SANIM : IFileFormat
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "Strikers Skeletal Animation" };
        public string[] Extension { get; set; } = new string[] { "*.sanim" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream) {
            return fileInfo.Extension == ".sanim";
        }

        public SANIM_Parser AnimFile;

        public void Load(Stream stream) {
            AnimFile = new SANIM_Parser(stream);
        }

        public void Save(Stream stream)
        {
        }
    }
}
