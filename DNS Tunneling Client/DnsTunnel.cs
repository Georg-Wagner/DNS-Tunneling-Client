using DnsClient;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DNS_Tunneling_Client
{

    public class DNSTunnel
    {
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, string[]>> SendQueue = new ConcurrentDictionary<string, ConcurrentDictionary<string, string[]>>();
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, string[]>> ReciveQueue = new ConcurrentDictionary<string, ConcurrentDictionary<string, string[]>>();
        private static Random rnd = new Random();
        private static string _IP;
        private static string _tunnelingDomain;
        private int _port;
        public string IP
        {
            get { return _IP; }
            set { _IP = value; }
        }
        public static string TunnelingDomain
        {
            get { return _tunnelingDomain; }
            set { _tunnelingDomain = value; }
        }
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
        public DNSTunnel(string IP, int port, string tunnelingDomain)
        {
           
            _IP = IP;
            _port = port;
            _tunnelingDomain = tunnelingDomain;
        }
        public DNSTunnel(string tunnelingDomain)
        {
            _IP = null;
            _port = 0;
            _tunnelingDomain = tunnelingDomain;
        }
        public static HashSet<byte[]> ReadStream(string streamId, string messageId) 
        {
            HashSet<byte[]> rtrn = new HashSet<byte[]>();
            try
            {

       
            //var thrd =  new Thread(() =>
            //  {
            //  Thread.CurrentThread.IsBackground = true;
            
                string[] streamQueue;
   a1:
            ConcurrentDictionary<string, string[]> stream = new ConcurrentDictionary<string, string[]>();
                while (!ReciveQueue.TryGetValue(streamId, out stream));
     
            

                    while (!ReciveQueue.TryGetValue(streamId, out stream));
                    while (!stream.TryGetValue(messageId, out streamQueue)) ;
                    while (streamQueue.Length == 0)
                    {
                            stream.TryGetValue(messageId, out streamQueue);
                    }
              
                try
                {
                    for (int i = 0; i < streamQueue.Length; i++)
                    {
                        while (string.IsNullOrEmpty(streamQueue[i]))
                        {
                            while (stream.TryGetValue(messageId, out streamQueue) == false) ;
                        }
                    }
                    while (streamQueue.All(x => string.IsNullOrEmpty(x)))
                    {
                        while (stream.TryGetValue(messageId, out streamQueue) == false) ;
                    }
                }
                catch (Exception)
                {
                    while (stream.TryGetValue(messageId, out streamQueue) == false) ;
                    goto a1;
                }
                string base64Recived = "";
                foreach (string hexLine in streamQueue)
                {
                    base64Recived += hexLine;
                }
                rtrn.Add(Convert.FromBase64String(base64Recived));
            
            //});
            //thrd.Start();
            //if (!thrd.Join(TimeSpan.FromSeconds(360)))
            //{
            //    thrd.Abort();
            //}
       
                
               
            }
            catch (Exception)
            {

            }
            return rtrn;
        }
        public static byte[] HexStringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        public static string AddMessageToSendQueue(string streamId, byte[] messageBytes, string host, Int32 port)
        {
            string messageHexString = ByteArrayToString(messageBytes);
            string hostPortHex = ByteArrayToString(Encoding.ASCII.GetBytes(host + ":" + port));
            string[] message = ConvertHexStringToDomainSendQueue(messageHexString, hostPortHex);
            string messageId = GenerateID();
            if (SendQueue.ContainsKey(streamId))
            {
                ConcurrentDictionary<string, string[]> streamMessagesOld = new ConcurrentDictionary<string, string[]>();
                ConcurrentDictionary<string, string[]> streamMessagesNew = new ConcurrentDictionary<string, string[]>();
                SendQueue.TryGetValue(streamId, out streamMessagesOld);
                SendQueue.TryGetValue(streamId, out streamMessagesNew);
                streamMessagesNew.TryAdd(messageId, message);
                SendQueue.TryUpdate(streamId, streamMessagesNew, streamMessagesOld);
            }
            else
            {
                ConcurrentDictionary<string, string[]> streamMessages = new ConcurrentDictionary<string, string[]>();
                streamMessages.TryAdd(messageId, message);
                SendQueue.TryAdd(streamId, streamMessages);
            }

            
            return messageId;
        }
        private static string[] ConvertHexStringToDomainSendQueue(string hexData, string hostPortHex)
        {
            int tunnelingDomainLenght = TunnelingDomain.Replace(".","").Length;
            int idLenght = 14;
            int packetCountLenght = 13;
            int maxHexDataLenght = 253 - tunnelingDomainLenght - idLenght - packetCountLenght;
            int domainsCount = hexData.Length / maxHexDataLenght +1;
            int labelsCount = maxHexDataLenght / 62;
            string[] splittedData = new string[domainsCount +1];

            if (hostPortHex.Length > 62)
                hostPortHex = hostPortHex.Insert(62, ".");
            if (hostPortHex.Length > 125)
                hostPortHex = hostPortHex.Insert(125, ".");
            if (hostPortHex.Length > 188)
                hostPortHex = hostPortHex.Insert(188, ".");
            splittedData[splittedData.Length - 1] = hostPortHex;


            string hexSubdomains;
            for (int i = 0; i < domainsCount; i++)
            {
                //   
                if (hexData.Length>maxHexDataLenght)
                {
                    hexSubdomains = hexData.Substring(0, maxHexDataLenght);
                    hexData = hexData.Remove(0, maxHexDataLenght);
                }
                else
                {
                    hexSubdomains = hexData.Substring(0, hexData.Length);
                    hexData = hexData.Remove(0, hexData.Length);
                }
                

                if (hexSubdomains.Length > 62)
                    hexSubdomains = hexSubdomains.Insert(62, ".");
                if (hexSubdomains.Length > 125)
                    hexSubdomains = hexSubdomains.Insert(125, ".");
                if (hexSubdomains.Length > 188)
                    hexSubdomains = hexSubdomains.Insert(188, ".");
                splittedData[i] = hexSubdomains;
                

            }
            splittedData[splittedData.Length - 1] = hostPortHex;
            return splittedData;
        }
        private static string GenerateID()
        {
             return rnd.Next(1, 16777215).ToString("x");             
        }
        public static string CreateStream(NetworkStream stream)
        {
            string streamID = GenerateID();
            DNSTunnelReciveQueue.AddNewStream(streamID, stream);
            return streamID;
        }
        private static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
        public void Start()
        {                                                                                                                                               
           // Thread.CurrentThread.IsBackground = true;
            var client = new LookupClient();
            if (_IP != null && _port != 0)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(_IP), _port);
                client = new LookupClient(endpoint);
            }
            client.UseCache = false;

            while (true)
            {
                foreach (string streamId in SendQueue.Keys)
                {

                    ConcurrentDictionary<string, string[]> stream = new ConcurrentDictionary<string, string[]>();
                    SendQueue.TryGetValue(streamId, out stream);

            //    new Thread(() =>
            //    {
            //Thread.CurrentThread.IsBackground = true;
            // string message = "";
                    string messageID;
                    IEnumerable<DnsClient.Protocol.TxtRecord> result = new List<DnsClient.Protocol.TxtRecord>();
                    if (!stream.IsEmpty || stream.Count != 0)
                    {
                        messageID = stream.Keys.First();
                        DNSTunnelReciveQueue.TryAdd(streamId, new ConcurrentDictionary<string, string[]>());
                        ConcurrentDictionary<string, string[]> reciveQueueStreamNew = new ConcurrentDictionary<string, string[]>();
                        ConcurrentDictionary<string, string[]> reciveQueueStreamOld = new ConcurrentDictionary<string, string[]>();
                        DNSTunnelReciveQueue.TryGetValue(streamId, out reciveQueueStreamNew);
                        DNSTunnelReciveQueue.TryGetValue(streamId, out reciveQueueStreamOld);

                        reciveQueueStreamNew.TryAdd(messageID, new string[] { });
                        DNSTunnelReciveQueue.TryUpdate(streamId, reciveQueueStreamNew, reciveQueueStreamOld);
                        string[] msg = new string[] { "" };
                    while (stream.TryRemove(messageID, out msg) == false) ;
                        SendQueue[streamId] = stream;
                    for (int i = 0; i < msg.Length; ++i)
                        {

                        try
                        {
                            //Thread.CurrentThread.IsBackground = true;
                           
                            string request = $"{msg[i]}.{messageID}-{streamId}.{i}-{msg.Length}.{_tunnelingDomain}";
                            //client.Timeout = System.TimeSpan.FromMilliseconds(100);

                            var result2 = client.QueryAsync(request, QueryType.TXT);
                        
                        // result = client.Query(request, QueryType.TXT).Answers.TxtRecords();
                        
                            foreach (var item in result2.Result.Answers.TxtRecords())
                              {
                                if (item.Text.First() != "null")
                                {
                                    ResultParser(item.Text.First());
                                }
                                                            
                               }
                        }
                        catch (Exception)
                        {

                        }
                            
                        }

                    }
                    else
                    {
                         try
                    {
                        messageID = GenerateID();
                       // while (ReciveQueue.TryAdd(messageID, null) == false) ;
                        result = client.Query($"{messageID}.0.{_tunnelingDomain}", QueryType.TXT).Answers.TxtRecords();
                   
                        foreach (var item in result)
                                                {
                            if (item.Text.First() != "null")
                            {
                                ResultParser(item.Text.First());
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                        
                    }
                    //          }).Start();


                }
            }
        }
        private void ResultParser(string result)
        {
            try
            {
                int pFrom = result.IndexOf("[") + "[".Length;
                int pTo = result.LastIndexOf("]");

                string msgInfo = result.Substring(pFrom, pTo - pFrom);
                string messageID = msgInfo.Split('.')[0].Split('-')[0];
                string streamID = msgInfo.Split('.')[0].Split('-')[1];

                int messageNum = int.Parse(msgInfo.Split('.')[1].Split('-')[0]);
                int messagesCount = int.Parse(msgInfo.Split('.')[1].Split('-')[1]);
                string message = result.Split(']')[1];
                DNSTunnelReciveQueue.Put(streamID, messageID, message, messagesCount, messageNum); //nezavisimo ot togo pustoe soobshenie ili net
                //ConcurrentDictionary<string, string[]> stream = new ConcurrentDictionary<string, string[]>();
                //ReciveQueue.TryGetValue(streamID, out stream);

                ////try
                ////{
                ////    if (stream[messageID] == null)
                ////    { 

                ////    }
                ////}
                ////catch (Exception)
                ////{
                ////    stream[messageID] = new string[messagesCount];
                ////}
                //if (stream[messageID] == null || stream[messageID].Length == 0)
                //{
                //    stream[messageID] = new string[messagesCount];
                //}
                //try
                //{
                //    stream[messageID][messageNum] = message;
                //  //  Console.WriteLine($"Recived ID: {messageID}, {messageNum} / {messagesCount}");
                //}
                //catch (Exception)
                //{

                //}
                ////ConcurrentDictionary<string, string[]> oldStream = new ConcurrentDictionary<string, string[]>();
                ////ReciveQueue.TryGetValue(streamID, out oldStream);
                ////ReciveQueue.TryUpdate(streamID, stream, oldStream);

            }
            catch (Exception)
            {

                
            }



        }
    }
}
