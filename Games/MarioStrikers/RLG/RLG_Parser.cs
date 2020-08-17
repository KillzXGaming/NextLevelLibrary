using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Toolbox.Core.IO;
using System.Linq;

namespace NextLevelLibrary.MarioStrikers
{
    public class RLG_Parser
    {
        Dictionary<SectionMagic, SectionHeader> SectionLookup = new Dictionary<SectionMagic, SectionHeader>();

        public bool IsGamecube = false;

        public RLG_Parser(Stream stream) {
            Read(new FileReader(stream));
        }

        public void Read(FileReader reader) {
            //Gather a list of hashes from our hash lists
            var HashList = Hashing.HashNames;

            reader.SetByteOrder(true);
            reader.ReadUInt32(); //magic
            reader.ReadUInt32(); //fileSize

            //Create a list of all the major sections 
            //The sections are not in order, so we must order them while parsing

            SectionLookup.Clear();
            while (!reader.EndOfStream) {
                uint magic = reader.ReadUInt32();
                uint sectionSize = reader.ReadUInt32();

                long pos = reader.Position;
                if (!SectionLookup.ContainsKey((SectionMagic)magic))
                    SectionLookup.Add((SectionMagic)magic, new SectionHeader(sectionSize, pos));

                //This section will skip sub sections so don't do that
                if (magic != 0x8001B000)
                    reader.SeekBegin(pos + sectionSize);

                if (!IsGamecube) //Align for RLG
                    reader.Align(4);
            }
            ParseModel(reader);
        }

        private void ParseModel(FileReader reader)
        {
            //Determine the size of the entries in the section chunks
            //Needed to find the amount of entries used in a chunk
            int modelSize = IsGamecube ? 16 : 12;
            int attributeSize = IsGamecube ? 6 : 8;

            //Get the model section and find the model amount
            var modelHeader = FindSeekSection(reader, SectionMagic.ModelData);
            uint numModels = modelHeader.Size / (uint)modelSize;

            //Parse each model chunk
            var models = new ModelChunk[numModels];
            for (int m = 0; m < numModels; m++) {
                models[m] = new ModelChunk(reader, IsGamecube);
            }

            //Next get the mesh data. We will need the total count to read each mesh entry.
            uint meshTotalCount = (uint)models.Sum(x => x.MeshCount);
            var meshData = FindSeekSection(reader, SectionMagic.MeshData);
            var meshSize = meshData.Size / meshTotalCount;
            var meshes = new MeshData[meshTotalCount];

            for (int i = 0; i < meshTotalCount; i++) {
                reader.SeekBegin(meshData.Position + (i * meshSize));
                meshes[i] = new MeshData(reader, IsGamecube);
            }

            var matrixChunk = FindSeekSection(reader, SectionMagic.MatrixData);
            if (matrixChunk != null)
            {

            }

            //Read the vertex attrbiutes
            var vertexAttributeChunk = FindSeekSection(reader, SectionMagic.VertexAttributePointerData);
            uint numAttributes = vertexAttributeChunk.Size / (uint)attributeSize;
            var attributes = new VertexAttribute[numAttributes];
            for (int i = 0; i < numAttributes; i++) {
                attributes[i] = new VertexAttribute(reader, IsGamecube);
            }
        }

        private SectionHeader FindSeekSection(FileReader reader, SectionMagic section)
        {
            if (SectionLookup.ContainsKey(section)) {
                var sectionHeader = SectionLookup[section];
                reader.SeekBegin(sectionHeader.Position);
                return sectionHeader;
            }
            return null;
        }
    }

    public class SectionHeader
    {
        public long Position;
        public uint Size;

        public SectionHeader(uint size, long pos)
        {
            Size = size;
            Position = pos;
        }
    }
}
