using System;
using System.IO;
using System.IO.Compression;

using Zstandard.Net;

namespace EasterLib
{
    public class GArchiveEntry
    {
        private GArchive _archive;
        private GArchiveEntryDescription.CompressionType _compressionType;
        private long _offset;

        internal GArchiveEntry(GArchive archive, GArchiveEntryDescription description)
        {
            _archive = archive;

            Name = Path.GetFileName(description.FullName);
            FullName = description.FullName;
            Length = description.Length;
            CompressedLength = description.CompressedLength;

            _compressionType = description.Compression;
            _offset = description.Offset;
        }

        public string Name { get; private set; }

        public string FullName { get; private set; }

        public long Length { get; private set; }

        public long CompressedLength { get; private set; }

        private Stream WrapWithDecompressor(Stream stream)
        {
            switch (_compressionType)
            {
                case GArchiveEntryDescription.CompressionType.None:
                    return stream;

                case GArchiveEntryDescription.CompressionType.LZ4:
                    throw new NotImplementedException();

                case GArchiveEntryDescription.CompressionType.ZSTD:
                    return new ZstandardStream(stream, CompressionMode.Decompress);

                default:
                    throw new ArgumentOutOfRangeException(nameof(_compressionType));
            }
        }

        private Stream OpenSubStream()
        {
            Stream compressedStream = new SubStream(_archive.ArchiveStream, _offset, CompressedLength);
            return WrapWithDecompressor(compressedStream);
        }

        public void ExtractToFile(string filepath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using (FileStream outputStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                using (Stream entryStream = OpenSubStream())
                {
                    entryStream.CopyTo(outputStream);
                }
            }
        }
    }
}
