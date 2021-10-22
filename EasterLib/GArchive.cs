using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace EasterLib
{
    public class GArchive : IDisposable
    {
        private static readonly UInt32 _magic = 0x6A37;

        private static readonly string CannotRead = "Stream does not support reading.";
        private static readonly string CannotWrite = "Stream does not support writing.";
        private static readonly string CannotSeek = "Stream does not support seeking.";
        private static readonly string CannotCreateInReadMode = "Cannot create entry for existing archive.";
        private static readonly string CannotWriteInReadMode = "Cannot write entries into existing archive.";
        private static readonly string CannotReadInCreateMode = "Cannot read entries for created archive.";

        private readonly Stream _archiveStream;
        private BinaryReader _archiveReader;
        private BinaryWriter _archiveWriter;
        private List<GArchiveEntry> _entries;
        private ReadOnlyCollection<GArchiveEntry> _entriesCollection;
        private bool isDisposed;
        private GArchiveMode _mode;

        public GArchive(Stream stream, GArchiveMode mode)
        {
            _archiveStream = stream;
            _archiveReader = null;
            _archiveWriter = null;
            _entries = new List<GArchiveEntry>();
            _entriesCollection = new ReadOnlyCollection<GArchiveEntry>(_entries);
            isDisposed = false;
            _mode = mode;

            switch (mode)
            {
                case GArchiveMode.Create:
                {
                    if (!_archiveStream.CanWrite)
                        throw new ArgumentException(CannotWrite);

                    if (!_archiveStream.CanSeek)
                        throw new ArgumentException(CannotSeek);

                    _archiveWriter = new BinaryWriter(_archiveStream);
                }
                break;

                case GArchiveMode.Read:
                {
                    if (!_archiveStream.CanRead)
                        throw new ArgumentException(CannotRead);

                    if (!_archiveStream.CanSeek)
                        throw new ArgumentException(CannotSeek);

                    _archiveReader = new BinaryReader(_archiveStream);

                    ReadEntries();
                }
                break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public ReadOnlyCollection<GArchiveEntry> Entries => _entriesCollection;

        internal Stream ArchiveStream => _archiveStream;

        internal GArchiveMode Mode => _mode;

        public GArchiveEntry CreateEntry(string source, string target, GArchiveEntry.CompressionType compressionType)
        {
            if (_mode == GArchiveMode.Read)
                throw new NotSupportedException(CannotCreateInReadMode);

            string targetUnixified = target.Replace('\\', '/');

            //TODO(adm244): maybe check for duplicates?

            GArchiveEntry createdEntry = new GArchiveEntry(this, source, targetUnixified, compressionType);

            _entries.Add(createdEntry);

            return createdEntry;
        }

        public void WriteEntries()
        {
            if (_mode == GArchiveMode.Read)
                throw new NotSupportedException(CannotWriteInReadMode);

            // write header

            _archiveWriter.Write((UInt32)_magic);
            _archiveWriter.Write((Int32)_entries.Count);

            long entriesStartOffset = _archiveWriter.BaseStream.Position;

            // write entries description

            var entriesDescription = new GArchiveEntryDescription[_entries.Count];
            for (int i = 0; i < _entries.Count; ++i)
            {
                entriesDescription[i] = GArchiveEntryDescription.Create(_entries[i]);
                entriesDescription[i].Write(_archiveWriter);
            }

            long dataStartOffset = _archiveWriter.BaseStream.Position;

            // write entries

            for (int i = 0; i < _entries.Count; ++i)
            {
                _entries[i].Write(
                    out entriesDescription[i].Offset,
                    out entriesDescription[i].Length,
                    out entriesDescription[i].CompressedLength
                );
            }

            // patch entries description (offset, size, zsize)

            _archiveWriter.BaseStream.Seek(entriesStartOffset, SeekOrigin.Begin);

            for (int i = 0; i < entriesDescription.Length; ++i)
            {
                entriesDescription[i].Write(_archiveWriter);
            }

            if (_archiveWriter.BaseStream.Position != dataStartOffset)
                throw new IOException();
        }

        private void ReadEntries()
        {
            if (_mode == GArchiveMode.Create)
                throw new NotSupportedException(CannotReadInCreateMode);

            if (_archiveReader.ReadUInt32() != _magic)
                throw new InvalidDataException();

            UInt32 filesCount = _archiveReader.ReadUInt32();
            for (int i = 0; i < filesCount; ++i)
            {
                var fileDescription = GArchiveEntryDescription.Read(_archiveReader);
                _entries.Add(new GArchiveEntry(this, fileDescription));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                _archiveStream.Dispose();
                _archiveReader?.Dispose();

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
