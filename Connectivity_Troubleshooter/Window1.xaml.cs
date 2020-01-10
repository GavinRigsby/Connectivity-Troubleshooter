using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;

namespace Connectivity_Troubleshooter
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>


    public partial class Window1 : Window, INotifyPropertyChanged
    {
        public Json_tool toolkit;
        public string ErrorMap;
        
        //threading to deal with loading bar and other running content
        private BackgroundWorker _bgWorker = new BackgroundWorker();

        private int _workerState;

        public event PropertyChangedEventHandler PropertyChanged;

        public int WorkerState
        {
            get { return _workerState; }
            set
            {
                _workerState = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("WorkerState"));
            }

        }

        public int GetNetworkStats(string host, int scantime)
        {
            int timeout = 5000;
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions(); // default is: don't fragment and 128 Time-to-Live

            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data); // 32 bytes of data

            var failedPings = 0;
            var latencySum = 0;

            Stopwatch timer = new Stopwatch();
            timer.Start();
            TimeSpan ts = timer.Elapsed;
            int pingAmount = 0;
            while (ts.Seconds < scantime)
            {
                System.Diagnostics.Debug.WriteLine(host);
                try
                {
                    PingReply reply = pingSender.Send(host, timeout, buffer, options);



                    if (reply != null)
                    {
                        if (reply.Status != IPStatus.Success)
                            failedPings += 1;
                        else
                            latencySum += (int)reply.RoundtripTime;
                    }
                }
                catch (PingException)
                {
                    failedPings += 1;
                }
                
                pingAmount += 1;

                System.Threading.Thread.Sleep(100);
                WorkerState = Convert.ToInt32(ts.Seconds);

                ts = timer.Elapsed;
            }

            //int averagePing = (latencySum / (pingAmount - failedPings));
            int packetLoss = Convert.ToInt32((Convert.ToDouble(failedPings) / Convert.ToDouble(pingAmount)) * 100);

            //MessageBox.Show("average " + Convert.ToString(averagePing) + "\npacket Loss " + Convert.ToString(packetLoss) + "\nfailed " + Convert.ToString(failedPings) + "\n amount " + Convert.ToString(pingAmount) + "\n latency " + Convert.ToString(pingAmount));

            //up
            if (packetLoss < 1)
            {
                return 1;
            }
            //unstable
            else if (1 <= packetLoss && packetLoss <= 5)
            {
                return 2;
            }
            //down
            else
            {
                return 0;
            }
        }

        
        private class Indicator
        {
            Grid wrapper = new Grid();
            Ellipse circle = new Ellipse();
            TextBlock ip_Addr = new TextBlock();
            SolidColorBrush up = new SolidColorBrush(Colors.Green);
            SolidColorBrush down = new SolidColorBrush(Colors.Red);
            SolidColorBrush unstable = new SolidColorBrush(Colors.Yellow);
            public Indicator()
            {
                this.ip_Addr.Text = "Test IP";
                this.circle.Width = 10;
                this.circle.Height = 10;
                this.circle.StrokeThickness = 1;
                circle.Fill = new SolidColorBrush(Colors.White);
                wrapper.Children.Add(ip_Addr);
                wrapper.Children.Add(circle);
            }

            public Indicator(string ip)
            {
                this.ip_Addr.Text = ip;
                this.circle.Width = 10;
                this.circle.Height = 10;
                this.circle.StrokeThickness = 1;
                circle.Fill = new SolidColorBrush(Colors.Yellow);
                wrapper.Children.Add(ip_Addr);
                wrapper.Children.Add(circle);
            }

            public Grid returnGrid()
            {
                return this.wrapper;
            }

            public void colorupdate(string ip, int state)
            {
                ip_Addr.Text = ip;
                
                if (state == 0)
                {
                    circle.Fill = this.down;
                    
                }
                else if (state == 1)
                {
                    circle.Fill = this.up;
                }
                else if (state == 2)
                {
                    circle.Fill = this.unstable;
                }


            }
        }


        

        public void make_Pings()
        {
            int tasks = 6;
            int running = 0;
            foreach(KeyValuePair<string,Dictionary<string,Dictionary<string,int>>> target in this.toolkit.targets)
            {
                string ipname = target.Key;
                Dictionary<string, int> Connection = target.Value["IP"];
                
                int value = 0;
                foreach (KeyValuePair<string, int> ip in Connection)
                {

                    running += 1;
                    string ipaddr = ip.Key;
                    int w = ip.Value;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.running.Text = "Test Running: " + ipname;
                        this.finished.Text = running.ToString() + "/" + tasks.ToString() + " Tests Run";
                    }));
                    int stage = GetNetworkStats(ipaddr, 30);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Indicator ind = new Indicator();
                        ind.colorupdate(ipname, stage);
                        this.Indicators.Children.Add(ind.returnGrid());
                    }));
                    if (stage == 1)
                    {
                        value = w;
                        break;
                    }
                    value = stage;
                    if (!Connection[ip.Key].Equals(Connection.Last().Value))
                    {
                        tasks += 1;
                    }

                }
                this.ErrorMap += value.ToString();
            }
            
            //runs arp and puts in arp.txt
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            string wd = Directory.GetCurrentDirectory();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "CMD.exe";
            startInfo.Arguments = "/c arp -a";
            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            //check pinging the gateway

            string gateway = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback).SelectMany(n => n.GetIPProperties()?.GatewayAddresses).Select(g => g?.Address).Where(a => a != null).FirstOrDefault().ToString();

            Dictionary<string, int> InternalScan = toolkit.js["Internal Scan"].ToObject<Dictionary<string, int>>();
            int scantime = InternalScan["ScanTime"];
            int depth = InternalScan["Network Depth"];
            running += 1;
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.running.Text = "Test Running: Default Gateway";
                this.finished.Text = running.ToString() + "/" + tasks.ToString() + " Tests Run";
            }));
            int gatestatus = GetNetworkStats(gateway, scantime);
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Indicator ind = new Indicator();
                ind.colorupdate("Default Gateway", gatestatus);
                this.Indicators.Children.Add(ind.returnGrid());
            }));

            

            string subnet = gateway.Substring(0, 8);



            //TextReader ARPin = new StreamReader(@"arp.txt");
            string strARP = output;
                /*ARPin.ReadToEnd();
            ARPin.Close();*/

            string internalIP = strARP.Substring(testARP(strARP, subnet, 2), 10);
            int Arpstatus = 0;
            int weight = 4;
            int devicesScans = 0;
            Dictionary<string, int> Devices = toolkit.js["Credit Unions"][toolkit.QAmap[1]]["Network Devices"].ToObject<Dictionary<string, int>>();
            if (Devices.Count > 0)
            {
                foreach (KeyValuePair<string, int> device in Devices)
                {
                    running += 1;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    { 
                        this.running.Text = "Test Running: Network Devices";
                        this.finished.Text = running.ToString() + "/" + tasks.ToString() + " Tests Run";
                    }));
                    Arpstatus = GetNetworkStats(device.Key, scantime);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Indicator ind = new Indicator();
                        ind.colorupdate("Network Devices", Arpstatus);
                        this.Indicators.Children.Add(ind.returnGrid());
                    }));
                    devicesScans += 1;
                    if (Arpstatus == 1)
                    {
                        weight = device.Value;
                    }
                    break;
                }
            }
            if (Arpstatus != 1)
            {
                running += 1;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.running.Text = "Test Running: Network Devices";
                    this.finished.Text = running.ToString() + "/" + tasks.ToString() + " Tests Run";
                }));
                Arpstatus = GetNetworkStats(internalIP, scantime);

                int scans = 1 + devicesScans;
                int occurance = 1;
                while (scans <= depth && (Arpstatus != 1))
                    scans += 1;
                    occurance += 1;
                    running += 1;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.running.Text = "Test Running: Network Devices";
                        this.finished.Text = running.ToString() + "/" + tasks.ToString() + " Tests Run";
                    }));
                    internalIP = strARP.Substring(testARP(strARP, subnet, occurance), 10);
                    Arpstatus = GetNetworkStats(internalIP, scantime);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Indicator ind = new Indicator();
                        ind.colorupdate("Network Devices", Arpstatus);
                        this.Indicators.Children.Add(ind.returnGrid());
                    }));
                weight = InternalScan["stage " + scans.ToString()];
            }

            int sqlopen = 2;
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect("127.0.0.1", 1433);
                    sqlopen = 1;
                    tcpClient.Close();
                }
                catch (Exception)
                {
                    sqlopen = 0;
                }
            }


            toolkit.NID = gatestatus.ToString() + weight.ToString() + sqlopen.ToString();
            this.ErrorMap += toolkit.NID;


            Task.Delay(2000).ContinueWith(_ =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.toolkit.Ernum = this.ErrorMap;
                    InfoWin Info = new InfoWin(this.toolkit);
                    Info.Show();
                    this.Close();

                }));
            }
            );
        }

        public int testARP(string strLog, string subnet, int occurence)
        {
            int itter = 1;
            int index = 0;
            while (itter <= occurence && (index = strLog.IndexOf(subnet, index + 1)) != -1)
            {
                if (itter == occurence)
                    return index;
                itter++;
            }
            return -1;
        }

        public Window1(Json_tool toolkit)
        {
            InitializeComponent();
            this.toolkit = toolkit;
            toolkit.Connections();
            DataContext = this;

            _bgWorker.DoWork += (s, e) =>
            {
                make_Pings();
            };

            _bgWorker.RunWorkerAsync();
        }
    }
}