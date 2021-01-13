using System;
using System.Collections.Generic;
using System.Text;
using System.Net;


namespace DNS_Tunneling_Client
{
    class localIP
    {

        public List<string> getNetworkInterface()
        {
            String strHostName = string.Empty;
            // Ermitteln der lokalen IP-Adressen 
            strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            List<string> ipList = new List<string>();
            for (int i = 0; i < addr.Length; i++)
            {
                if (!addr[i].IsIPv6LinkLocal)
                {
                    ipList.Add(addr[i].ToString());
                }
            }
            ipList.Add("127.0.0.1");
            return ipList;
            
        }





    }
}
