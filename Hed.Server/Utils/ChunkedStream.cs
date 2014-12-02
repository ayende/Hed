using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hed.Server.Utils
{
    public class ChunkedStream : Stream
    {
        private readonly Stream _stream;

        public override void Flush()
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

        private const int _bufferSize = 16;
        private readonly byte[] _internalBuffer = new byte[_bufferSize];
        private int _posInBuffer = 0;

        public ChunkedStream(Stream stream)
        {
            _stream = stream;
        }

        private bool _done;
        private int _nextChunkLen = -1;
        private int _nextHeaderLen = -1;
        private int _byteRead = 0;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_done)
                return 0;

            int read;
            if (_nextHeaderLen > 0)
            {
                read = Math.Min(count, _nextHeaderLen);
                Buffer.BlockCopy(_internalBuffer, _posInBuffer, buffer, offset, read);
                _nextHeaderLen -= read;
                _posInBuffer += read;
                return read;
            }

            if (_nextChunkLen > 0)
            {
                read = await _stream.ReadAsync(buffer, offset, Math.Min(_nextChunkLen, count),cancellationToken);
                if (read == 0)
                {
                    _done = true;// also shouldn't happen
                    return 0;
                }
                _nextChunkLen -= read;
                return read;
            }

            read = await _stream.ReadAsync(_internalBuffer, _byteRead, _bufferSize - _byteRead, cancellationToken);
            var actualRead = read + _byteRead; //we copied those bytes from before.
            _byteRead = 0;
            _posInBuffer = 0;
            if (actualRead == 0)
            {
                _done = true;// actually shouldn't happen, but still
                return 0;
            }
            var rnPos = -1;
            for (int i = 1; i < actualRead - 1; i++)
            {
                if (_internalBuffer[i] == '\r' && _internalBuffer[i + 1] == '\n')
                {
                    rnPos = i;
                    break;
                }
            }
            if (rnPos == -1)
                throw new FormatException("Can't understand the chunked encoding, no \\r\\n found");

            var num = Encoding.UTF8.GetString(_internalBuffer, 0, rnPos);
            var len = int.Parse(num, NumberStyles.HexNumber);
            if (len == 0)
            {
                _done = true;
                return 0;
            }

            var fullChunkHeaderSize = len + rnPos + 2 + 2;
            if (fullChunkHeaderSize <= actualRead)
            {
                //_posInBuffer = read - (len + rnPos + 1);
                _nextChunkLen = -1;
                _nextHeaderLen = -1;
                Buffer.BlockCopy(_internalBuffer, _posInBuffer, buffer, offset, fullChunkHeaderSize);
                Buffer.BlockCopy(_internalBuffer, _posInBuffer + fullChunkHeaderSize, _internalBuffer, 0, actualRead - fullChunkHeaderSize);
                _byteRead = actualRead - fullChunkHeaderSize;
                return fullChunkHeaderSize;
            }

            _nextChunkLen = fullChunkHeaderSize - actualRead;

            _nextHeaderLen = actualRead;
            actualRead = Math.Min(count, _nextHeaderLen);
            Buffer.BlockCopy(_internalBuffer, _posInBuffer, buffer, offset, actualRead);
            _nextHeaderLen -= actualRead;
            return actualRead;
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}
