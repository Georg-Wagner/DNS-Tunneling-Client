using DnsClient;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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

        public static string AddMessageToSendQueue(string streamId, byte[] messageBytes, string host, Int32 port)
        {
            //Daten werden in hexadezimale Form konvertiert, um als Subdomains verwendet zu werden
            string messageHexString = ByteArrayToString(messageBytes);
            string hostPortHex = ByteArrayToString(Encoding.ASCII.GetBytes(host + ":" + port));
            //Browser-Anfrage wird auf mehrere DNS-anfragen geteilt und nummeriert
            string[] message = ConvertHexStringToDomainSendQueue(messageHexString, hostPortHex);
            // MessageID und StreamID ermöglichen zurückgegebene Antworten vom Tunneling-Server zuzuordnen
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
            //Ermitteln der maximalen Größe von Daten, die in einer DNS-Anfrage verschickt werden kann
            int tunnelingDomainLenght = TunnelingDomain.Replace(".","").Length;
            int idLenght = 14;
            int packetCountLenght = 13;
            int maxHexDataLenght = 253 - tunnelingDomainLenght - idLenght - packetCountLenght;
            int domainsCount = hexData.Length / maxHexDataLenght +1;
            int labelsCount = maxHexDataLenght / 62;
            string[] splittedData = new string[domainsCount +1];
            // Da die Subdomains nicht länger als 63 Zeichen sein dürfen, wird auch der Zielhost, falls er länger ist, aufgeteilt
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
                
                //Teilung auf Subdomains
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
            Thread.CurrentThread.IsBackground = true;
            var dnsLookupClient = new LookupClient();
            dnsLookupClient.Timeout = TimeSpan.FromSeconds(1000);
            if (_IP != null && _port != 0)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(_IP), _port);
                dnsLookupClient = new LookupClient(endpoint);
            }
            dnsLookupClient.UseCache = false;

            while (true)
            {
                foreach (string streamId in SendQueue.Keys)
                {

                    ConcurrentDictionary<string, string[]> stream = new ConcurrentDictionary<string, string[]>();
                    SendQueue.TryGetValue(streamId, out stream);

                    string messageID;
                   
                    if (!stream.IsEmpty || stream.Count != 0)
                    {
                        //MessageID und StreamID werden von der Versandwarteschlange gelöscht und zur Empfangswarteschlange hinzugefügt
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
                            
                            string request = $"{msg[i]}.{messageID}-{streamId}.{i}-{msg.Length}.{_tunnelingDomain}";
                            var result = dnsLookupClient.QueryAsync(request, QueryType.TXT);
                            foreach (var item in result.Result.Answers.TxtRecords())
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
                        // Da die zurückgegebenen Daten vom Server in der Regel größer als die von der Browser-Anfrage sind, es werden "leere" Pakete in den DNS-Anfragen verschickt um Downstream zu ermöglichen. Auch wenn es keine neuen Browser-Anfragen gibt
                       var result = dnsLookupClient.Query($"{messageID}.0.{_tunnelingDomain}", QueryType.TXT).Answers.TxtRecords();
                   
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
                DNSTunnelReciveQueue.Put(streamID, messageID, message, messagesCount, messageNum); 

            }
            catch (Exception)
            {

                
            }



        }
    }
}
