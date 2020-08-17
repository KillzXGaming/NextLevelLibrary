using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core.IO;

namespace NextLevelLibrary.LM2
{
    public class CutsceneChunk
    {
        public static void Read(Stream stream) {
            Read(new FileReader(stream, true));
        }

        static void Read(FileReader reader)
        {
            reader.ReadSignature(4, "NMLB");
            uint unk = reader.ReadUInt32();
            uint stringTableOffset = reader.ReadUInt32();

            reader.SeekBegin(stringTableOffset);
            while (!reader.EndOfStream) {
                string str = reader.ReadZeroTerminatedString();
             //   Console.WriteLine(str);
            }
        }
    }
}
