using System;
using System.IO;

namespace EasterLib.Archive
{
    internal class WriteOnlySubStream : Stream
    {
        private Stream _superStream;
        private long _superStreamOffsetStart;
        private long _superStreamPosition;

        public WriteOnlySubStream(Stream superStream, long offset)
        {
            if (superStream == null)
                throw new ArgumentNullException(nameof(superStream));

            _superStream = superStream;
            _superStreamOffsetStart = offset;
            _superStreamPosition = offset;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => _superStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _superStreamPosition - _superStreamOffsetStart;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_superStream.Position != _superStreamPosition)
                _superStream.Seek(_superStreamPosition, SeekOrigin.Begin);

            _superStream.Write(buffer, offset, count);

            _superStreamPosition += count;
        }

        public override void Flush()
        {
            //NOTE(adm244): just ignore it
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
