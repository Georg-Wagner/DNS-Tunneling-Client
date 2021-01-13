using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
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
            //Überprüfung, ob diese Antwort vom Tunneling-Server der letzte Teil für diese messageID ist....
            if (messagesCount - 1 == messageNum)
            {
                string[] messages;
                byte[] dataRecivedFromDNSTunnel;
                while (!stream.TryGetValue(messageID, out messages));
                // Es wird überprüft, ob tatsächlich alle Teile angekommen sind
                if (!messages.All(x => string.IsNullOrEmpty(x)))
                {
                    //Antworten werden zusammengefügt und in einen byte array konvertiert
                    string base64Recived = "";
                    foreach (string base64Line in messages)
                    {
                        base64Recived += base64Line;
                    }
                    dataRecivedFromDNSTunnel = Convert.FromBase64String(base64Recived);

                    //Es wird der passende Networkstream rausgesucht
                    NetworkStream clientStream;
                    Streams.TryGetValue(streamID, out clientStream);

                new Thread(async () =>
                {
                    Log.Information($"Recived Size: {dataRecivedFromDNSTunnel.Length}, ID: {streamID}");
                    // Antwort wird an den Browser´zurückgegeben
                    try
                    {
                        await clientStream.WriteAsync(dataRecivedFromDNSTunnel);
                    }
                    catch (Exception)
                    {
                    }
                 

                }).Start();
                }    

            }

        }

    }
}
