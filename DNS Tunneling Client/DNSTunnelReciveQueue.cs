using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DNS_Tunneling_Client
{
  static  class DNSTunnelReciveQueue
    {
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, string[]>> ReciveQueue = new ConcurrentDictionary<string, ConcurrentDictionary<string, string[]>>();
        private static ConcurrentDictionary<string, NetworkStream> Streams = new ConcurrentDictionary<string, NetworkStream>();
        public static void AddNewStream(string streamID, NetworkStream stream)
        {
            Streams.TryAdd(streamID, stream);
        }
        public static bool TryGetValue(string streamID, out ConcurrentDictionary<string, string[]> stream)
        {
           return ReciveQueue.TryGetValue(streamID, out stream);
        }
        public static bool TryAdd(string streamId, ConcurrentDictionary<string, string[]> stream)
        {
           return ReciveQueue.TryAdd(streamId, stream);
        }
        public static bool TryUpdate(string streamId, ConcurrentDictionary<string, string[]> newStream, ConcurrentDictionary<string, string[]> oldStream)
        {
            return ReciveQueue.TryUpdate(streamId, newStream, oldStream);
        }
        public static void Put(string streamID, string messageID, string message, int messagesCount, int messageNum)
        {
            ConcurrentDictionary<string, string[]> stream = new ConcurrentDictionary<string, string[]>();
            ReciveQueue.TryGetValue(streamID, out stream);

            try
            {
            if (stream[messageID] == null || stream[messageID].Length == 0)
            {
                stream[messageID] = new string[messagesCount];
            }
            
                stream[messageID][messageNum] = message;
                  Console.WriteLine($"Recived ID: {messageID}, {messageNum} / {messagesCount}");
            }
            catch (Exception)
            {

            }
            if (messagesCount - 1 == messageNum)
            {
                string[] messages;
                byte[] dataRecivedFromDNSTunnel;
                while (!stream.TryGetValue(messageID, out messages));
                if (!messages.All(x => string.IsNullOrEmpty(x)))
                {
                    string base64Recived = "";
                    foreach (string hexLine in messages)
                    {
                        base64Recived += hexLine;
                    }
                    dataRecivedFromDNSTunnel = Convert.FromBase64String(base64Recived);

                    NetworkStream clientStream;
                    Streams.TryGetValue(streamID, out clientStream);

                new Thread(async () =>
                {
                    Log.Information($"Recived Size: {dataRecivedFromDNSTunnel.Length}, ID: {streamID}");
                    await clientStream.WriteAsync(dataRecivedFromDNSTunnel);

                }).Start();
                }    

            }

        }

    }
}
