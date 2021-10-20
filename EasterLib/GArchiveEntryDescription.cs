using System;
using System.IO;

using EasterLib.Extensions;

namespace EasterLib
{
    internal struct GArchiveEntryDescription
    {
        public string FullName;
        public UInt32 Offset;
        public GArchiveEntry.CompressionType Compression;
        public UInt32 Length;
        public UInt32 CompressedLength;

        internal static GArchiveEntryDescription Create(GArchiveEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            //NOTE(adm244): be ware of data loss (int64 -> uint32)
            // maybe we shouldn't support > uint32 at all?

            return new GArchiveEntryDescription()
            {
                FullName = entry.FullName,
                Offset = (UInt32)entry.Offset,
                Compression = entry.Compression,
                Length = (UInt32)entry.Length,
                CompressedLength = (UInt32)entry.CompressedLength
            };
        }

        internal static GArchiveEntryDescription Read(BinaryReader reader)
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

        internal void Write(BinaryWriter writer)
        {
            writer.WriteCString(FullName);
            writer.Write((UInt32)Offset);
            writer.Write((UInt32)Compression);
            writer.Write((UInt32)Length);
            writer.Write((UInt32)CompressedLength);
        }

        private static GArchiveEntry.CompressionType MapCompressionValue(UInt32 value)
        {
            if (!Enum.TryParse<GArchiveEntry.CompressionType>(value.ToString(), out GArchiveEntry.CompressionType result))
                throw new ArgumentOutOfRangeException();

            return result;
        }
    }
}
