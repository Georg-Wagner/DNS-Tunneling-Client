using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNS_Tunneling_Client
{
    public partial class Form1 : Form
    {
        Thread socksProxyThread;
        Thread tunnelingThread;
        public Form1()
        {
            InitializeComponent();
        }
        public void ProxyProcess(string ip, int port)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Start the Proxy Server");

            Proxy proxy = new Proxy(ip, port);
            proxy.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            localIP localIP = new localIP();
            List<string> ipList = localIP.getNetworkInterface();
            foreach (var ip in ipList)
            {
                cboListOfIPs.Items.Add(ip);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string ip = cboListOfIPs.Text;
            int port = Convert.ToInt32(numPort.Value);
            if (btnStart.BackColor != Color.Firebrick)
            {
                cboListOfIPs.Enabled = false;
                numPort.Enabled = false;
            socksProxyThread =    new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    ProxyProcess(ip, port);
                });
                socksProxyThread.Start();
                DNSTunnel tunnel = new DNSTunnel(tbxDomain.Text);

             tunnelingThread =   new Thread(() =>
                {
                    
                    tunnel.Start();
                });
                tunnelingThread.Start();

                btnStart.BackColor = Color.Firebrick;
                btnStart.Text = "Exit Tunnel";
            }
            else
            {
                Application.Exit();
                btnStart.BackColor = Color.Teal;
                btnStart.Text = "Start";
            }
           


        }
      
    }
}
