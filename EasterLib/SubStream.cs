using System;
using System.IO;

namespace EasterLib
{
    internal class SubStream : Stream
    {
        private Stream _superStream;
        private long _superStreamOffsetStart;
        private long _superStreamOffsetEnd;
        private long _superStreamPosition;
        private long _length;

        public SubStream(Stream superStream, long offset, long length)
        {
            _superStream = superStream;
            _superStreamOffsetStart = offset;
            _superStreamOffsetEnd = offset + length;
            _superStreamPosition = offset;
            _length = length;
        }

        public override bool CanRead { get { return _superStream.CanRead; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return _superStreamOffsetEnd - _superStreamOffsetStart; } }

        public override long Position
        {
            get
            {
                return _superStreamPosition - _superStreamOffsetStart;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_superStream.Position != _superStreamPosition)
                _superStream.Seek(_superStreamPosition, SeekOrigin.Begin);

            if (_superStreamPosition + count > _superStreamOffsetEnd)
                count = (int)(_superStreamOffsetEnd - _superStreamPosition);

            int bytesRead = _superStream.Read(buffer, offset, count);

            _superStreamPosition += bytesRead;

            return bytesRead;
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
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
