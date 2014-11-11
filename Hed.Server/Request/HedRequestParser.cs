using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Hed.Server.Connection;
using Hed.Server.Utils;
using HttpMachine;

namespace Hed.Server.Request
{
    internal class HedRequestParser
    {
        private sealed class ParseDelegate : IHttpParserHandler
        {
            private string headerName;

            public HedRequest request = new HedRequest();
            public ArraySegment<byte> requestBodyStart;
            public bool complete;
            public bool headerComplete;

            void IHttpParserHandler.OnBody(HttpParser parser, ArraySegment<byte> data) { requestBodyStart = data; }
            void IHttpParserHandler.OnFragment(HttpParser parser, string fragment) { }
            void IHttpParserHandler.OnHeaderName(HttpParser parser, string name) { headerName = name; }
            void IHttpParserHandler.OnHeaderValue(HttpParser parser, string value) { request.Headers.Add(headerName, value); }
            void IHttpParserHandler.OnHeadersEnd(HttpParser parser) { this.headerComplete = true; }
            void IHttpParserHandler.OnMessageBegin(HttpParser parser) { }
            void IHttpParserHandler.OnMessageEnd(HttpParser parser) { this.complete = true; }
            void IHttpParserHandler.OnMethod(HttpParser parser, string method) { request.Method = method; }
            void IHttpParserHandler.OnQueryString(HttpParser parser, string queryString) { }
            void IHttpParserHandler.OnRequestUri(HttpParser parser, string requestUri) { request.RequestUri = requestUri; }
        }

        public HedRequestParser()
        {
        }

        public async Task<HedRequest> ParseAsync(InboundConnection conn, Stream stream)
        {
            var del = new ParseDelegate();
            var parser = new HttpParser(del);

            int read;
            int readTotal = 0;
            byte[] buffer = new byte[8192];

            Debug.WriteLine(string.Format("{0}: RequestParser starting", conn.RemoteEndPoint));

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                readTotal += read;

                if (parser.Execute(new ArraySegment<byte>(buffer, 0, read)) != read)
                    throw new FormatException("Parse error in request");

                if (del.headerComplete)
                    break;
            }

            Debug.WriteLine(string.Format("{0}: RequestParser read enough ({1} bytes)", conn.RemoteEndPoint, readTotal));

            if (readTotal == 0)
                return null;

            if (!del.headerComplete)
                throw new FormatException("Parse error in request");

            var request = del.request;

            request.ProtocolVersion = new Version(parser.MajorVersion, parser.MinorVersion);

            int cl = request.ContentLength;

            if (cl > 0)
            {
                if (del.requestBodyStart.Count > 0)
                {
                    request.RequestBody = new MaxReadStream(new StartAvailableStream(del.requestBodyStart, stream), cl);
                }
                else
                {
                    request.RequestBody = new MaxReadStream(stream, cl);
                }
            }

            return request;
        }
    }
}
