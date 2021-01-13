using System;
using System.Net.Sockets;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
namespace DNS_Tunneling_Client
{
    class Connection
    {
        private Int32 port;
        private String host;

        private TcpClient tcpClient;
        private NetworkStream upstream;
        private NetworkStream clientStream;

        public Connection(String host, Int32 port, TcpClient tcpClient)
        {
            this.port = port;
            this.host = host;
            this.clientStream = tcpClient.GetStream();
            this.tcpClient = tcpClient;

        }

        public void Close()
        {
            upstream.Close();
            tcpClient.Close();
            clientStream.Close();
        }



        public async Task Response()
        {
            try
            {
                Log.Information("connect to upstream: {0}:{1}", host, port);
                List<byte> dataToSendList = new List<byte>();
                byte[] dataRecivedFromBrowser = new byte[4096];
                HashSet<byte[]> dataRecivedFromDNSTunnel = new HashSet<byte[]>();

                //StreamID ist nötig um lokalen Networkstream von dem Browser mit dem Networkstream auf dem DNS-Server/Tunneling-Server zu synchronisieren
                string streamId = DNSTunnel.CreateStream(clientStream);
                new Thread(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            if (clientStream.DataAvailable)
                            {

                            int recivedBytesCountFromBrowser = 0;
                            
                            recivedBytesCountFromBrowser = await clientStream.ReadAsync(dataRecivedFromBrowser);
                            if (recivedBytesCountFromBrowser != 0)
                            {

                                dataRecivedFromBrowser = dataRecivedFromBrowser.Take(recivedBytesCountFromBrowser).ToArray();
                                    // Browseranfrage wird zu der Warteschlange des DNS-Streams hinzugefügt
                               DNSTunnel.AddMessageToSendQueue(streamId, dataRecivedFromBrowser, host, port);
                                Log.Information($"Added to SendQueue Size: {dataRecivedFromBrowser.Length}, ID: {streamId}, {host}:{port}");
                            }

                            }
                        }
                        catch (Exception)
                        {

                        }

                    }


                }).Start();

            }
            catch (IOException e)
            {
                try
                {
                    Close();
                }
                catch (Exception)
                {

                }

                Log.Information("connect to upstream {0}:{1} error: ", host, port, e.Message);
            }
        }
       
    }
}
