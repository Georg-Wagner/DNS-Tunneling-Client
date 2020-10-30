﻿using DnsClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DNS_Tunneling_Client
{

    public class DNSTunnel
    {
        public static ConcurrentDictionary<string, string[]> SendQueue = new ConcurrentDictionary<string, string[]>();
        public static ConcurrentDictionary<string, string[]> ReciveQueue = new ConcurrentDictionary<string, string[]>();
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
        public static byte[] ReadStream(string streamId) 
        {
          var thrd =  new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                string[] streamQueue;
                while (ReciveQueue.TryGetValue(streamId, out streamQueue) == false) ;
                a1:
                try
                {

                    while (streamQueue.All(x => !string.IsNullOrEmpty(x)))
                    {
                        while (ReciveQueue.TryGetValue(streamId, out streamQueue) == false) ;
                    }
                }
                catch (Exception)
                {
                    while (ReciveQueue.TryGetValue(streamId, out streamQueue) == false) ;
                    goto a1;
                }

            });
            thrd.Start();
            if (!thrd.Join(TimeSpan.FromSeconds(360)))
            {
                thrd.Abort();
            }

                return new byte[] { };
        }
        public static string AddMessageToSendQueue(byte[] messageBytes, string host, Int32 port)
        {
            string messageHexString = ByteArrayToString(messageBytes);
            string hostPortHex = ByteArrayToString(Encoding.ASCII.GetBytes(host + ":" + port));
            string[] message = ConvertHexStringToDomainSendQueue(messageHexString, hostPortHex);
            string generatedID = GenerateID();
            SendQueue.TryAdd(generatedID, message);
            return generatedID;
        }
        private static string[] ConvertHexStringToDomainSendQueue(string hexData, string hostPortHex)
        {
            int tunnelingDomainLenght = TunnelingDomain.Replace(".","").Length;
            int idLenght = 6;
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
        private static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
        public void Start()
        {
            Thread.CurrentThread.IsBackground = true;
            var client = new LookupClient();
            if (_IP != null && _port != 0)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(_IP), _port);
                client = new LookupClient(endpoint);
            }
            client.UseCache = false;
            
            while (true)
            {

                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    // string message = "";
                    string messageID;
                    IEnumerable<DnsClient.Protocol.TxtRecord> result = new List<DnsClient.Protocol.TxtRecord>();
                    if (!SendQueue.IsEmpty)
                    {
                        messageID = SendQueue.Keys.First();
                        while (ReciveQueue.TryAdd(messageID, null) == false) ;
                        string[] msg = new string[] { "" };
                    while (SendQueue.TryRemove(messageID, out msg) == false) ;

                    for (int i = 0; i < msg.Length; ++i)
                        {

try
                        {
                            //Thread.CurrentThread.IsBackground = true;
                            /* run your code here */
                            string request = $"{msg[i]}.{messageID}.{i}-{msg.Length}.{_tunnelingDomain}";
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
               }).Start();
              

                
            }
        }
        private void ResultParser(string result)
        {
            try
            {
                int pFrom = result.IndexOf("[") + "[".Length;
                int pTo = result.LastIndexOf("]");

                string msgInfo = result.Substring(pFrom, pTo - pFrom);
                string messageID = msgInfo.Split('.')[0];
                int messageNum = int.Parse(msgInfo.Split('.')[1].Split('-')[0]);
                int messagesCount = int.Parse(msgInfo.Split('.')[1].Split('-')[1]);
                string message = result.Split(']')[1];
                if (ReciveQueue[messageID] == null)
                {
                    ReciveQueue[messageID] = new string[messagesCount];
                }
                try
                {
                    ReciveQueue[messageID][messageNum] = message;
                }
                catch (Exception)
                {

                }
            }
            catch (Exception)
            {

                
            }



        }
    }
}