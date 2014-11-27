using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Hed.ConsoleHost
{
    public class SimpleProxy : IDisposable
    {
        private readonly TcpListener _listener;

        public SimpleProxy(int port)
        {
            _listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));
            _listener.Start();

            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var acceptTcpClient = _listener.AcceptTcpClient();
                    var networkStream = acceptTcpClient.GetStream();
                    while (networkStream.DataAvailable)
                    {
                        networkStream.ReadByte();
                    }
                    var streamWriter = new StreamWriter(networkStream);
                    streamWriter.Write("HTTP/1.1 200 OK\r\n\r\n");
                    streamWriter.Flush();
                    networkStream.Flush();
                    acceptTcpClient.Close();

                    Console.WriteLine(i);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            //Task.Run(() => HandleConenction(_listener.AcceptTcpClientAsync()));
        }

        private async Task HandleConenction(Task<TcpClient> acceptTcpClientAsync)
        {
            var buffer = new byte[2048];
            int read = 0;
            using (var tcpClient = await acceptTcpClientAsync)
            {
                Console.WriteLine("here");
                //Task.Run(() => HandleConenction(_listener.AcceptTcpClientAsync()));
                //using (var stream = tcpClient.GetStream())
                //{
                //    while (true)
                //    {
                //        int r = stream.Read(buffer, read, buffer.Length - read);
                //        if (r == 0)
                //            break;
                //        read += r;
                //    }
                //    int lineBreak = -1;
                //    lineBreak = SearchAfterNewLineInBuffer(buffer, lineBreak, read);
                //    if (lineBreak == -1)
                //        throw new ArgumentException("Cannot find \r\n in the first 2Kb");
                //    var urlStart = SearchAfterSpaceInBuffer(buffer,0, lineBreak);
                //    if (urlStart == -1)
                //        throw new ArgumentException("Cannot find space in buffer");
                //    var urlEnd = SearchAfterSpaceInBuffer(buffer, urlStart, lineBreak);
                //    if (urlEnd == -1)
                //        throw new ArgumentException("Cannot find 2nd space in buffer");

                //    var url = Encoding.UTF8.GetString(buffer, urlStart, urlEnd);

                //}
            }
        }

        private static int SearchAfterNewLineInBuffer(byte[] buffer, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (buffer[i] == '\r' && i + 1 < end && buffer[i + 1] == '\n')
                {
                    return i + 1;
                }
            }
            return -1;
        }

        private static int SearchAfterSpaceInBuffer(byte[] buffer, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (buffer[i] == '0' && i+1 < end)
                {
                    return i + 1;
                }
            }
            return -1;
        }
        public void Dispose()
        {
            if (_listener != null)
            {
                _listener.Stop();
            }

        }
    }
}