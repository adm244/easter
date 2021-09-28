using System;
using System.IO;
using System.IO.Compression;

using EasterLib.Extensions;

using Zstandard.Net;

namespace EasterLib
{
    enum Compression
    {
        None,
        LZ4, // https://github.com/MiloszKrajewski/K4os.Compression.LZ4
        ZSTD // https://github.com/bp74/Zstandard.Net
    }

    struct FilesTable
    {
        public string Name;
        public UInt32 Offset;
        public Compression Compression;
        public UInt32 Size;
        public UInt32 SizeCompressed;
    }

    public static class Package
    {
        private static readonly UInt32 ArchiveSignature = 0x6A37;

        public static void Unpack(string filepath)
        {
            //TODO(adm244): check if filepath is NOT a directory

            string archiveName = Path.GetFileNameWithoutExtension(filepath);

            using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // Verify archive signature
                    UInt32 signature = reader.ReadUInt32();
                    if (signature != ArchiveSignature)
                        throw new NotImplementedException();

                    // Read files table
                    UInt32 filesCount = reader.ReadUInt32();
                    FilesTable[] files = new FilesTable[filesCount];
                    for (int i = 0; i < files.Length; ++i)
                    {
                        files[i] = new FilesTable()
                        {
                            Name = reader.ReadCString(),
                            Offset = reader.ReadUInt32(),
                            Compression = (Compression)reader.ReadUInt32(),
                            Size = reader.ReadUInt32(),
                            SizeCompressed = reader.ReadUInt32()
                        };
                    }

                    // Extract files
                    for (int i = 0; i < files.Length; ++i)
                    {
                        string outputPath = Path.Join(archiveName, files[i].Name);

                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                        using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            using (Stream subStream = new SubStream(stream, files[i].Offset, files[i].SizeCompressed))
                            {
                                if (files[i].Compression == Compression.ZSTD)
                                {
                                    using (Stream subStreamZStd = new ZstandardStream(subStream, CompressionMode.Decompress))
                                    {
                                        subStreamZStd.CopyTo(outputStream);
                                    }
                                }
                                else
                                {
                                    subStream.CopyTo(outputStream);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
