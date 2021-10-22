using System;
using System.IO;
using System.IO.Compression;

using Zstandard.Net;

namespace EasterLib.Archive
{
    public class GArchiveEntry
    {
        private GArchive _archive;
        private CompressionType _compressionType;
        private long _offset;
        private string _sourcepath;

        internal GArchiveEntry(GArchive archive, string source, string target, CompressionType compressionType)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            source = Path.GetFullPath(source);

            _archive = archive;
            _sourcepath = source;
            _compressionType = compressionType;

            Name = Path.GetFileName(target);
            FullName = target;
            Length = 0;
            CompressedLength = 0;
        }

        internal GArchiveEntry(GArchive archive, GArchiveEntryDescription description)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            _archive = archive;

            Name = Path.GetFileName(description.FullName);
            FullName = description.FullName;
            Length = description.Length;
            CompressedLength = description.CompressedLength;

            _compressionType = description.Compression;
            _offset = description.Offset;
        }

        public string Name { get; }

        public string FullName { get; }

        public long Length { get; }

        public long CompressedLength { get; }

        internal long Offset => _offset;

        internal CompressionType Compression => _compressionType;

        private Stream WrapWithCompressor(Stream stream)
        {
            CompressionMode mode = GetCompressionMode();

            switch (_compressionType)
            {
                case CompressionType.None:
                    return stream;

                case CompressionType.LZ4:
                    throw new NotImplementedException();

                case CompressionType.ZSTD:
                    return new ZstandardStream(stream, mode);

                default:
                    throw new ArgumentOutOfRangeException(nameof(_compressionType));
            }
        }

        private CompressionMode GetCompressionMode()
        {
            switch (_archive.Mode)
            {
                case GArchiveMode.Create:
                    return CompressionMode.Compress;

                case GArchiveMode.Read:
                    return CompressionMode.Decompress;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_archive.Mode));
            }
        }

        public Stream Open()
        {
            Stream entryStream;

            switch (_archive.Mode)
            {
                case GArchiveMode.Create:
                {
                    entryStream = new WriteOnlySubStream(_archive.ArchiveStream, _archive.ArchiveStream.Position);
                }
                break;

                case GArchiveMode.Read:
                {
                    entryStream = new ReadOnlySubStream(_archive.ArchiveStream, _offset, CompressedLength);
                }
                break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_archive.Mode));
            }

            return WrapWithCompressor(entryStream);
        }

        internal void Write(out UInt32 offset, out UInt32 length, out UInt32 compressedLength)
        {
            using (FileStream stream = new FileStream(_sourcepath, FileMode.Open, FileAccess.Read))
            {
                using (Stream entryStream = Open())
                {
                    offset = (UInt32)_archive.ArchiveStream.Position;

                    stream.CopyTo(entryStream);

                    //NOTE(adm244): in case entryStream haven't written its content into the base stream
                    //entryStream.Flush();

                    length = (UInt32)stream.Position;
                }

                compressedLength = (UInt32)_archive.ArchiveStream.Position - offset;
            }
        }

        public void ExtractToFile(string filepath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using (FileStream outputStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                using (Stream entryStream = Open())
                {
                    entryStream.CopyTo(outputStream);
                }
            }
        }

        public enum CompressionType
        {
            None = 0,
            LZ4 = 1,
            ZSTD = 2
        }
    }
}
