using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;

namespace NextLevelLibrary
{
    public class DICT : ObjectTreeNode, IArchiveFile, IFileFormat, IDisposable
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "LM2/LM3 Archive" };
        public string[] Extension { get; set; } = new string[] { "*.dict" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                reader.SetByteOrder(true);
                return reader.ReadUInt32() == 0x5824F3A9;
            }
        }

        public bool CanAddFiles { get; set; } = false;
        public bool CanRenameFiles { get; set; } = false;
        public bool CanReplaceFiles { get; set; } = false;
        public bool CanDeleteFiles { get; set; } = false;

        public void ClearFiles() { }

        public IEnumerable<ArchiveFileInfo> Files => DictFile.BlockList;

        public IDictionaryData DictFile;
        public DATA_Parser DataFile;

        public GameVersion Version = GameVersion.LM2;

        public enum GameVersion
        {
            LM2,
            LM3,
        }

        public static Dictionary<uint, DATA_Parser.FileEntry> GlobalFileList = new Dictionary<uint, DATA_Parser.FileEntry>();

        private Stream _stream;
        private Stream _dataStream;

        public void Load(Stream stream)
        {
            _stream = stream;
            using (var reader = new FileReader(stream, true)) {
                reader.SetByteOrder(true);
                reader.SeekBegin(12);
                if (reader.ReadUInt32() == 0x78340300)
                    Version = GameVersion.LM3;
            }

            Tag = this;

            //Parse dictionary
            if (Version == GameVersion.LM3)
                DictFile = new LM3.DICT_Parser(stream);
            else
                DictFile = new LM2.DICT_Parser(stream);
            this.Label = FileInfo.FileName;

            DictFile.FilePath = FileInfo.FilePath;

            //Parse seperate data file
            string dataPath = FileInfo.FilePath.Replace(".dict", ".data");
            if (File.Exists(dataPath)) {
                _dataStream = File.OpenRead(dataPath);
                DataFile = new DATA_Parser(_dataStream, Version, DictFile);

                var root = LoadChunkTabe();
                Children.AddRange(root.Children);

                int index = 0;

                var subNode = new ObjectTreeNode("Data Entries");
                AddChild(subNode);
                foreach (var child in DataFile.Table.DataEntries) {
                    subNode.AddChild(ReadChunk(child));
                }
            }
        }

        private ObjectTreeNode LoadChunkTabe()
        {
            ObjectTreeNode root = new ObjectTreeNode();

            Dictionary<ChunkFileType, ObjectTreeNode> FileContainers = new Dictionary<ChunkFileType, ObjectTreeNode>();
            Dictionary<uint, DATA_Parser.FileEntry> animationFiles = new Dictionary<uint, DATA_Parser.FileEntry>();
            foreach (var file in DataFile.Files)
            {
                if (!animationFiles.ContainsKey(file.Hash))
                    animationFiles.Add(file.Hash, file);
                if (!GlobalFileList.ContainsKey(file.Hash))
                    GlobalFileList.Add(file.Hash, file);
            }

            Dictionary<string, ObjectTreeNode> fileGrouper = new Dictionary<string, ObjectTreeNode>();

            foreach (var file in DataFile.Files)
            {
                ChunkFileType type = file.ChunkEntry.ChunkType;
                if (!FileContainers.ContainsKey(type))
                {
                    FileContainers.Add(type, new ObjectTreeNode(type.ToString() + $"_{type.ToString("X")}"));
                    root.AddChild(FileContainers[type]);
                }

                var folder = FileContainers[file.ChunkEntry.ChunkType];
                var fileNode = new ObjectTreeNode(file.Hash.ToString());
                fileNode.Tag = file.ChunkEntry.Data;

                if (type == ChunkFileType.Model)
                    fileNode = new ModelWrapper(file, DataFile);
                if (type == ChunkFileType.AnimationData)
                    fileNode = new AnimationWrapper(file, DataFile);
                if (type == ChunkFileType.Texture)
                    fileNode = new TextureWrapper(file, DataFile);
                if (type == ChunkFileType.Skeleton)
                    fileNode = new SkeletonWrapper(file, DataFile);
                if (type == ChunkFileType.Script)
                    fileNode = new ScriptWrapper(file, DataFile);
                if (type == ChunkFileType.AnimationBundles)
                    fileNode = new AnimationBundleWrapper(file, DataFile, animationFiles);

                //Attempt to group common hashes
                if (type == ChunkFileType.Model) {
                    fileGrouper.Add(fileNode.Label, fileNode);
                }

                if (type != ChunkFileType.Model && fileGrouper.ContainsKey(fileNode.Label)) {
                    ObjectTreeNode tfolder = new ObjectTreeNode(type.ToString());
                    tfolder.AddChild(fileNode);
                    fileGrouper[fileNode.Label].AddChild(tfolder);
                }
                else
                    folder.AddChild(fileNode);

                if (type == ChunkFileType.Model)
                {
                    ObjectTreeNode chunkList = new ObjectTreeNode("Chunks");
                    fileNode.AddChild(chunkList);

                    foreach (var child in file.ChunkEntry.SubData)
                        chunkList.AddChild(ReadChunk(child));

                }
                else
                {
                    foreach (var child in file.ChunkEntry.SubData)
                        fileNode.AddChild(ReadChunk(child));
                }

                fileNode.Label += $"_({folder.ChildCount})";
                if (ExtensionList.ContainsKey(file.ChunkEntry.ChunkType))
                    fileNode.Label += ExtensionList[file.ChunkEntry.ChunkType];

                if (Hashing.HashNames.ContainsKey(file.Hash)) {
                    fileNode.Label = Hashing.HashNames[file.Hash];
                }
            }
            return root;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _dataStream?.Dispose();
        }

       private ObjectTreeNode ReadChunk(ChunkTable.ChunkDataEntry entry)
        {
            var subNode = new ObjectTreeNode(entry.ChunkType.ToString() + $"_{entry.ChunkType.ToString("X")}_Flags_{entry.Flags}_OFFSET_{entry.ChunkOffset}_SIZE_{entry.ChunkSize}");
            subNode.Tag = entry.Data;
            foreach (var child in entry.SubData)
                subNode.AddChild(ReadChunk(child));
            return subNode;
        }

        //Confirmed extensions used for existing hash matched files.
        Dictionary<ChunkFileType, string> ExtensionList = new Dictionary<ChunkFileType, string>()
        {
            { ChunkFileType.Model, ".nlg" },
            { ChunkFileType.Skeleton, ".nlg" },
            { ChunkFileType.AnimationBundles, ".bank.xml" },
            { ChunkFileType.Script, ".script" },
            { ChunkFileType.Video, ".webm" },
        };

        public void Save(Stream stream) {
            if (DataFile == null) return;

            var path = FileInfo.FilePath;

            //Save the data first

            using (var fileStream = new FileStream(path.Replace(".dict", ".data"), 
                FileMode.Create, FileAccess.Write, FileShare.Write)) 
            {
                using (var writer = new FileWriter(fileStream)) {
                    List<uint> offsets = new List<uint>();

                    foreach (var block in DictFile.BlockList) {
                        if (offsets.Contains(block.Offset) || block.SourceIndex != 0)
                            continue;

                        block.CompressedSize = 0;
                        block.Offset = (uint)writer.Position;
                        block.DecompressData(block.FileData).CopyTo(fileStream);
                        writer.Align(4);

                        offsets.Add(block.Offset);
                    }
                }
            }
            //Last save the dictionary
            if (Version == GameVersion.LM3)
                ((LM3.DICT_Parser)DictFile).Save(stream);
            else if (Version == GameVersion.LM2)
                ((LM2.DICT_Parser)DictFile).Save(stream);
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo) {
            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo) {
            return false;
        }
    }
}
