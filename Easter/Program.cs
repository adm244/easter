using System;
using System.Collections.Generic;
using System.IO;

using EasterLib.Archive;
using EasterLib.Files.PGF;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Easter
{
    class Program
    {
        static void Main(string[] args)
        {
            //PGFDecode(args[0], args[0] + ".png");
            //PGFEncode(args[0], args[0] + ".pgf");

            //NOTE(adm244): packed json is just a "message pack" thing:
            // https://github.com/msgpack/msgpack/blob/master/spec.md

            //string inputDirectory = Path.GetFullPath(args[0]);
            //string outputDirectory = Path.GetFullPath(args[1]);

            //UnpackAll(inputDirectory, outputDirectory);
            //PackAll(inputDirectory, outputDirectory);

            Image image = PGFDecoder.Decode(args[0]);
            image.SaveAsPng(args[0] + ".png");

            PGFDecoder.Encode((Image<Rgba32>)image, args[0] + ".png" + ".pgf");

            Image image2 = PGFDecoder.Decode(args[0] + ".png" + ".pgf");
            image2.SaveAsPng(args[0] + ".png" + ".pgf" + ".png");
        }

        private static void PackAll(string inputDirectory, string outputDirectory)
        {
            string[] folders = Directory.GetDirectories(inputDirectory);
            for (int i = 0; i < folders.Length; ++i)
            {
                string archiveName = Path.GetFileName(Path.TrimEndingDirectorySeparator(folders[i]));
                string outputFile = string.Format("{0}{1}", Path.Combine(outputDirectory, archiveName), ".g");

                Console.Write("Packing {0}...", archiveName);

                try
                {
                    Pack(folders[i], outputFile, GArchiveEntry.CompressionType.ZSTD);
                    Console.WriteLine(" Done!");
                }
                catch
                {
                    Console.WriteLine(" Failure!");
                }
            }
        }

        private static void Pack(string inputDirectory, string outputFile, GArchiveEntry.CompressionType compressionType)
        {
            string[] files = GetAllFilesRecursive(inputDirectory);

            using (FileStream stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                using (GArchive archive = new GArchive(stream, GArchiveMode.Create))
                {
                    for (int i = 0; i < files.Length; ++i)
                    {
                        string relativePath = Path.GetRelativePath(inputDirectory, files[i]);
                        archive.CreateEntry(files[i], relativePath, compressionType);
                    }

                    archive.WriteEntries();
                }
            }
        }

        private static string[] GetAllFilesRecursive(string directory)
        {
            List<string> files = new List<string>();

            files.AddRange(Directory.GetFiles(directory));

            string[] subDirectories = Directory.GetDirectories(directory);
            for (int i = 0; i < subDirectories.Length; ++i)
            {
                files.AddRange(GetAllFilesRecursive(subDirectories[i]));
            }

            return files.ToArray();
        }

        private static void UnpackAll(string inputDirectory, string outputDirectory)
        {
            string[] archiveFiles = Directory.GetFiles(inputDirectory, "*.g", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < archiveFiles.Length; ++i)
            {
                Console.Write("Extracting {0}...", Path.GetFileName(archiveFiles[i]));

                try
                {
                    Unpack(archiveFiles[i], outputDirectory);
                    Console.WriteLine(" Done!");
                }
                catch
                {
                    Console.WriteLine(" Failure!");
                }
            }
        }

        private static void Unpack(string sourceFile, string targetDirectory)
        {
            using (FileStream stream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                string archiveName = Path.GetFileNameWithoutExtension(sourceFile);

                using (GArchive archive = new GArchive(stream, GArchiveMode.Read))
                {
                    foreach (GArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(targetDirectory, archiveName, entry.FullName));
                        entry.ExtractToFile(destinationPath);
                    }
                }
            }
        }
    }
}
