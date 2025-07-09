using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace rePaCK_TKS
{
    public class PCK
    {
        //A PCK file is stored the following way: first we have an int with the number of files, then we have a table with all
        //the file entries, each entry containing a dummy field (which is always 0), the offset to its corresponding data,
        //and the size of the file in bytes.
        //After that we have the names of the files, each one ending with a null byte, and finally the file data itself. Said
        //file data section is stored in the order of the entries in the table.
        public class Entry
        {
            public string FileName { get; set; }
            public required uint Offset { get; set; }
            public required uint Size { get; set; }
        }

        /// <summary>
        /// Unpacks a PCK file to the specified directory.
        /// </summary>
        public static void Unpack(string pckPath, string outputDIR)
        {
            using (var fs = new FileStream(pckPath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                uint fileCount = br.ReadUInt32(); //First comes the file count
                var entries = new List<Entry>(); //We create a list to hold the entries

                //Read file table
                for (int currentFile = 0; currentFile < fileCount; currentFile++)
                {
                    uint dummy = br.ReadUInt32(); //Dummy field, always 0
                    if (dummy != 0) //Health check to ensure that we are reading a valid PCK file
                        throw new Exception("Invalid PCK: one of the entries is formatted incorrectly.");
                    uint offset = br.ReadUInt32();  //Offset of the file data
                    uint size = br.ReadUInt32();    //Amount of bytes in the file
                    entries.Add(new Entry { Offset = offset, Size = size });
                }

                //Read filenames
                for (int currentFile = 0; currentFile < fileCount; currentFile++)
                {
                    var nameBytes = new List<byte>(); //Array of bytes to hold the filename
                    byte b;
                    while ((b = br.ReadByte()) != 0) //Read until we hit a null byte
                        nameBytes.Add(b);
                    entries[currentFile].FileName = Encoding.UTF8.GetString(nameBytes.ToArray());
                }

                //Extract files
                foreach (var entry in entries)
                {
                    fs.Seek(entry.Offset, SeekOrigin.Begin);
                    byte[] data = br.ReadBytes((int)entry.Size);
                    string outPath = Path.Combine(outputDIR, entry.FileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    File.WriteAllBytes(outPath, data);
                }
            }
        }

        /// <summary>
        /// Packs multiple files into a PCK archive.
        /// </summary>
        public static void Pack(string[] filePaths, string outputPCKPath)
        {
            uint fileCount = (uint)filePaths.Length;
            var entries = new List<Entry>();
            var filenames = new List<string>();

            //Calculate header and file table size
            long offset = 4 + fileCount * 12; //Header + File entries (each entry is 12 bytes: 4 for dummy, 4 for offset, 4 for size)
            foreach (var path in filePaths)
            {
                string name = Path.GetFileName(path);
                filenames.Add(name);
                offset += Encoding.UTF8.GetByteCount(name) + 1; //+1 for zero terminator
            }

            //Prepare entries
            long currentOffset = offset;
            foreach (var path in filePaths)
            {
                //First we obtain all of the metadata regarding each file to pack
                long size = new FileInfo(path).Length;
                entries.Add(new Entry
                {
                    FileName = Path.GetFileName(path),
                    Offset = (uint)currentOffset,
                    Size = (uint)size
                });
                currentOffset += size; //This offset will be used for the next file, so we keep adding the size
            }

            //Now we write PCK file itself, since we have all of the entries prepared
            using (var fs = new FileStream(outputPCKPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(fileCount);

                //Write file table
                foreach (var entry in entries)
                {
                    bw.Write((uint)0); //Dummy bytes, always 0
                    bw.Write(entry.Offset); //Offset of the file data
                    bw.Write(entry.Size);   //Size of the file in bytes
                }

                //Write filenames
                foreach (var name in filenames)
                {
                    bw.Write(Encoding.UTF8.GetBytes(name));
                    bw.Write((byte)0); //Null terminator for the filename
                }

                //Write file data. Since there is no separation between files, we write them in the order of the entries
                foreach (var path in filePaths)
                {
                    byte[] data = File.ReadAllBytes(path);
                    bw.Write(data);
                }
            }
        }
    }
}
