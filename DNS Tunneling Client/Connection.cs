﻿using System;
using System.Net.Sockets;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;


namespace DNS_Tunneling_Client
{
    class Connection
    {
        private Int32 port = 80;
        private String host;

        private TcpClient tcpClient;
        private NetworkStream upstream;
        private NetworkStream clientStream;

        public Connection(String host, Int32 port, NetworkStream clientStream)
        {
            this.port = port;
            this.host = host;
          // var test = clientStream.ReadByte();
            this.clientStream = clientStream;


        }

        public void Close()
        {
            upstream.Close();
            tcpClient.Close();
            clientStream.Close();
        }

        public void CopyTo()
        {
            try
            {
                clientStream.CopyTo(upstream);
            }
            catch (ObjectDisposedException)
            {
                Log.Information("the reader has closed");
            }
            catch (IOException)
            {
                Log.Information("the reader has closed");
            }
        }

        public async Task Response()
        {
            try
            {
                // 建立新连接
               
                    
                Log.Information("connect to upstream: {0}:{1}", host, port);
                byte[] dataToSend = new byte[] { };
                using (MemoryStream ms = new MemoryStream())
                {
                    clientStream.CopyTo(ms);
                    dataToSend = ms.ToArray();
                }
               // TODO peredavat host i port
                string StreamId = DNSTunnel.AddMessageToSendQueue(dataToSend,host,port);
                byte[] recivedBytes =  DNSTunnel.ReadStream(StreamId);
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port);
                upstream = tcpClient.GetStream();
                upstream.ReadTimeout = 3000;

                // 独立线程，完成自己的任务后消失
                var thread = new Thread(CopyTo);
                thread.Start();

                await upstream.CopyToAsync(clientStream);
                Close();
            }
            catch (IOException e)
            {
                Close();
                Log.Information("connect to upstream {0}:{1} error: ", host, port, e.Message);
            }
        }
    }
}
