using System;
using System.IO;

using EasterLib.Extensions;

namespace EasterLib
{
    internal struct GArchiveEntryDescription
    {
        public string FullName;
        public UInt32 Offset;
        public CompressionType Compression;
        public UInt32 Length;
        public UInt32 CompressedLength;

        public static GArchiveEntryDescription Read(BinaryReader reader)
        {
            return new GArchiveEntryDescription()
            {
                FullName = reader.ReadCString(),
                Offset = reader.ReadUInt32(),
                Compression = MapCompressionValue(reader.ReadUInt32()),
                Length = reader.ReadUInt32(),
                CompressedLength = reader.ReadUInt32()
            };
        }

        private static CompressionType MapCompressionValue(UInt32 value)
        {
            if (!Enum.TryParse<CompressionType>(value.ToString(), out CompressionType result))
                throw new ArgumentOutOfRangeException();

            return result;
        }

        public enum CompressionType
        {
            None,
            LZ4,
            ZSTD
        }
    }
}
