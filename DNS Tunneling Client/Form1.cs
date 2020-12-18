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
        public Form1()
        {
            InitializeComponent();
        }
        public static void ProxyProcess()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Start the Proxy Server");

            Proxy proxy = new Proxy("127.0.0.1", 8008);
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
            var task = Task.Factory.StartNew(ProxyProcess, TaskCreationOptions.LongRunning);
            DNSTunnel tunnel1 = new DNSTunnel(tbxDomain.Text);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                tunnel1.Start();
            }).Start();

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.BackColor != Color.Firebrick)
            {
                btnStart.BackColor = Color.Firebrick;
                btnStart.Text = "Stop";
            }
            else
            {
                btnStart.BackColor = Color.Teal;
                btnStart.Text = "Start";
            }
           


        }
    }
}
