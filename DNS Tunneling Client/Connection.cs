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
          // var test = clientStream.ReadByte();
            this.clientStream = tcpClient.GetStream();
            this.tcpClient = tcpClient;

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
                List<byte> dataToSendList = new List<byte>();
                byte[] dataRecivedFromBrowser = new byte[4096];
                HashSet<byte[]> dataRecivedFromDNSTunnel = new HashSet<byte[]>();


                string streamId = DNSTunnel.CreateStream(clientStream);
                new Thread(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            if (clientStream.DataAvailable)
                            {

                            
                           // byte[] dataToSendArr = new byte[4096];
                            int recivedBytesCountFromBrowser = 0;
                            
                            recivedBytesCountFromBrowser = await clientStream.ReadAsync(dataRecivedFromBrowser);
                            if (recivedBytesCountFromBrowser != 0)
                            {
                                dataRecivedFromBrowser = dataRecivedFromBrowser.Take(recivedBytesCountFromBrowser).ToArray();
                               string messageId =  DNSTunnel.AddMessageToSendQueue(streamId, dataRecivedFromBrowser, host, port);
                                Log.Information($"Added to SendQueue Size: {dataRecivedFromBrowser.Length}, ID: {streamId}, {host}:{port}");
                              //  dataRecivedFromDNSTunnel = DNSTunnel.ReadStream(streamId, messageId);
                              //  Log.Information($"Recived Size: {dataRecivedFromDNSTunnel.Length}, ID: {streamId}, {host}:{port}");
                            }

                                //  await upstream.WriteAsync(dataToSendArr);
                            }
                        }
                        catch (Exception)
                        {

                        }

                    }


                }).Start();


                //new Thread(async () =>
                //{
                //    bool cleanDataRecivedfromDNSTunnel = false;
                //    while (true)
                //    {
                //        try
                //        {
                //            foreach (byte[] data in dataRecivedFromDNSTunnel)
                //            {
                //                await clientStream.WriteAsync(data);
                //                cleanDataRecivedfromDNSTunnel = true;
                //            }
                //            if (cleanDataRecivedfromDNSTunnel)
                //            {
                //                dataRecivedFromDNSTunnel = new HashSet<byte[]>();
                //                cleanDataRecivedfromDNSTunnel = false;
                //            }
                            
                           
                //        }
                //        catch (Exception)
                //        {
                //        }

                //    }
                //}).Start();

                //// clientStream.Write(recivedBytes);
                ////  clientStream.Close();
                //TcpClient tcpClientUpstream = new TcpClient();
                //await tcpClientUpstream.ConnectAsync(host, port);
                //upstream = tcpClientUpstream.GetStream();
                ////upstream.ReadTimeout = 3000;
                //await clientStream.CopyToAsync(upstream);
                //// await upstream.WriteAsync(dataToSendArr, 0, dataToSendArr.Length);
                ////byte[] dataBytesRecived = new byte[] { };
                ////using (MemoryStream ms = new MemoryStream())
                ////{
                ////    upstream.CopyTo(ms);
                ////    dataBytesRecived = ms.ToArray();
                ////}
                //await upstream.CopyToAsync(clientStream);
                ////    await upstream.WriteAsync(dataBytesRecived, 0, dataBytesRecived.Length);
                ////// 独立线程，完成自己的任务后消失
                ////var thread = new Thread(CopyTo);
                ////thread.Start();

                ////await upstream.CopyToAsync(clientStream);
                //Close();
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
        public async Task Response1()
        {
            try
            {
                
                // 建立新连接
                Log.Information("connect to upstream: {0}:{1}", host, port);
          
                //byte[] dataToSendArr = new byte[] { };
                //using (MemoryStream ms = new MemoryStream())
                //{
                //    clientStream.CopyTo(ms);
                //    dataToSendArr = ms.ToArray();
                //}
                //Log.Information($"Got {dataToSendArr.Length} bytes to send");
                //await upstream.WriteAsync(dataToSendArr);
                //Log.Information($"Sent {dataToSendArr.Length} bytes to {host}");
                //var thread = new Thread(CopyTo);
                //thread.Start();
                
                  // clientStream.CopyTo(upstream);


                  //using (MemoryStream ms = new MemoryStream())
                  //       {
                  //           clientStream.CopyTo(ms);
                  //           dataToSendArr = ms.ToArray();
                  //       }
                  TcpClient tcpClient_up = new TcpClient();
                await tcpClient_up.ConnectAsync(host, port);
                Log.Information($"Connected to {host}:{port}");
                upstream = tcpClient_up.GetStream();
               // upstream.ReadTimeout = 3000;
                new Thread(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            byte[] dataToSendArr = new byte[4096];
                            int recivedBytes = 0;
                            //recivedBytes = tcpClient.Client.Receive(dataToSendArr);
                            recivedBytes = clientStream.Read(dataToSendArr);
                            dataToSendArr = dataToSendArr.Take(recivedBytes).ToArray();
                            //  clientStream.CopyTo(upstream);
                            await upstream.WriteAsync(dataToSendArr);
                            // tcpClient_up.Client.Send(dataToSendArr);
                        }
                        catch (Exception)
                        {

                        }
                      
                    }


                }).Start();
                new Thread(async () =>
                {
                      while (true)
                      {
                        try
                        {
                            byte[] dataToSendArr1 = new byte[4096];
                            int recivedBytes = 0;
                            recivedBytes = upstream.Read(dataToSendArr1);
                            dataToSendArr1 = dataToSendArr1.Take(recivedBytes).ToArray();
                            await clientStream.WriteAsync(dataToSendArr1);
                        }
                        catch (Exception)
                        {
                        }
                   
                       }
                }).Start();

               // await upstream.CopyToAsync(clientStream);
              // Log.Information($"Trsnsfered {dataToSendArr.Length} bytes from {host} to Browser");
              //  Close();
                Log.Information($"Closed connection with {host}");
            }
            catch (IOException e)
            {
                Close();
                Log.Information("connect to upstream {0}:{1} error: ", host, port, e.Message);
            }
        }
    }
}
