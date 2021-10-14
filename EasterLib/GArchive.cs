using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace EasterLib
{
    public class GArchive : IDisposable
    {
        private static readonly UInt32 _magic = 0x6A37;

        private readonly Stream _archiveStream;
        private BinaryReader _archiveReader;
        private List<GArchiveEntry> _entries;
        private ReadOnlyCollection<GArchiveEntry> _entriesCollection;
        private bool isDisposed;

        public GArchive(Stream stream, GArchiveMode mode)
        {
            _archiveStream = stream;
            _archiveReader = null;
            _entries = new List<GArchiveEntry>();
            _entriesCollection = new ReadOnlyCollection<GArchiveEntry>(_entries);
            isDisposed = false;

            if (mode == GArchiveMode.Read)
            {
                _archiveReader = new BinaryReader(_archiveStream);
                ReadEntries();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ReadOnlyCollection<GArchiveEntry> Entries => _entriesCollection;

        internal Stream ArchiveStream => _archiveStream;

        private void ReadEntries()
        {
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
