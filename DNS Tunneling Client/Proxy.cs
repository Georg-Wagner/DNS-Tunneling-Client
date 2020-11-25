// https://samsclass.info/122/proj/how-socks5-works.html

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Serilog;
using System.IO;
using System.Threading;

namespace DNS_Tunneling_Client
{
    class SocksVersionException : Exception
    {
        public SocksVersionException(String message) : base(message)
        {
        }
    }

    class SocksDomainException : Exception
    {
        public SocksDomainException(String message) : base(message)
        {
        }
    }

    class SocksUpsteamPortException : Exception
    {
        public SocksUpsteamPortException(String message) : base(message)
        {
        }
    }

    public class Proxy
    {
        private TcpListener tcpListener;

        public Proxy(String bindIp, Int32 port)
        {
            IPAddress localAddr = IPAddress.Parse(bindIp);
            tcpListener = new TcpListener(localAddr, port);
        }

        public Int32 ParseSocksVersion(NetworkStream stream)
        {
            Int32 numbytesToRead = 3;
            Int32 numberOfbyteshasRead = 0;
            var bytes = new byte[3];
            do
            {
                Int32 n = stream.Read(bytes, numberOfbyteshasRead, numbytesToRead);
                numberOfbyteshasRead += n;
                numbytesToRead -= n;
            } while (numbytesToRead > 0);

            if (bytes[0] != 5)
                throw new SocksVersionException("socks version is wrong");

            stream.Write(new byte[] { 0x5, 0x0 }, 0, 2);
            return bytes[0];
        }

        public string GetHost(NetworkStream stream, byte flag, TcpClient client)
        {
            var bytes = new byte[1024];

            switch (flag)
            {
                // 如果采用的是域名
                case 0x03:
                    // 获取域名长度
                    var numbytesToRead = 1;
                    var numberOfbyteshasRead = 0;
                    Array.Clear(bytes, 0, bytes.Length);
                    do
                    {
                        Int32 n = stream.Read(bytes, numberOfbyteshasRead, numbytesToRead);
                        numbytesToRead -= n;
                        numberOfbyteshasRead += n;
                    } while (numbytesToRead > 0);

                    // 获取域名
                    numbytesToRead = bytes[0];
                    numberOfbyteshasRead = 0;
                    Array.Clear(bytes, 0, bytes.Length);
                    do
                    {
                        Int32 n = stream.Read(bytes, numberOfbyteshasRead, numbytesToRead);
                        numbytesToRead -= n;
                        numberOfbyteshasRead += n;
                    } while (numbytesToRead > 0);

                 //   Log.Logger.Information("Socks5: upstream domian is {0}",
                    //    Encoding.ASCII.GetString(bytes, 0, numberOfbyteshasRead));
                    var domain = Encoding.ASCII.GetString(bytes, 0, numberOfbyteshasRead);
                    return domain;

                case 0x01:
                    break;

                default:
                    throw new SocksDomainException("can not get upstream domain or ip");
            }

            return null;
        }

        public Int32 GetPort(NetworkStream stream)
        {
            Int32 numbytesToRead = 2;
            Int32 numberOfbyteshasRead = 0;
            var bytes = new byte[2];
            Array.Clear(bytes, 0, bytes.Length);
            do
            {
                Int32 n = stream.Read(bytes, numberOfbyteshasRead, numbytesToRead);
                numberOfbyteshasRead += n;
                numbytesToRead -= n;
            } while (numbytesToRead > 0);
            if (numberOfbyteshasRead == 0)
                throw new SocksUpsteamPortException("can not parse upstream port");
           // Log.Logger.Information("Socks5: upstream port {0}", bytes[0] * 256 + bytes[1]);
            return bytes[0] * 256 + bytes[1];
        }

        public void ResponseToSocks(NetworkStream stream)
        {
            var sockResponse = new byte[] {
                0x05, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00,
                0x1f, 0x40
            };
            stream.Write(sockResponse, 0, sockResponse.Length);
        }

        public async void Run(NetworkStream stream, TcpClient client)
        {
            try
            {
               
                // 解析 socks 头
                Int32 numbytesToRead = 4;
                Int32 numberOfbyteshasRead = 0;
                var bytes = new byte[4];
                var version = ParseSocksVersion(stream);
                do
                {
                    Int32 n = stream.Read(bytes, numberOfbyteshasRead, numbytesToRead);
                    numberOfbyteshasRead += n;
                    numbytesToRead -= n;
                } while (numbytesToRead > 0);

                // Tak ne rabotaet
                //int recivedBuffreSize = (int)client.ReceiveBufferSize;
                //byte[] clientbytes = new byte[recivedBuffreSize];
                //int bytesRead = stream.Read(clientbytes, 0, recivedBuffreSize);

                //string clientStream = BitConverter.ToString(clientbytes, 0, clientbytes.Length);
                //Console.WriteLine(clientStream);

                var conn = new Connection(GetHost(stream, bytes[3], client), GetPort(stream), client);
                ResponseToSocks(stream);


                // 转发请求并获取响应
                await conn.Response();
            }
            catch (IOException e)
            {
                Log.Logger.Information(e.Message);
            }
            catch (SocksVersionException e)
            {
                Log.Logger.Information(e.Message);
            }
            catch (SocksDomainException e)
            {
                Log.Logger.Information(e.Message);
            }
            catch (SocksUpsteamPortException e)
            {
                Log.Logger.Information(e.Message);
            }
            catch (Exception e)
            {
                Log.Logger.Information(e.Message);
            }
        }

        public void Start()
        {
            tcpListener.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Run(stream, client);
                }).Start();
                
            }
        }
    }
}
