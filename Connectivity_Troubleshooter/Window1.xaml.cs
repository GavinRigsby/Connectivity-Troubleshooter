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
using System.Collections.Concurrent;
using System.Threading;

namespace Connectivity_Troubleshooter
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public delegate void VoidDelegate();

    public static class Utils
    {
        public static void Try(VoidDelegate v)
        {
            try
            {
                v();
            }
            catch { }
        }
    }


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

        public void GetNetworkStats(string host, int scantime, ConcurrentQueue<Dictionary<string,string>> queue, int weight)
        {
            string[] identifier = host.Split('|');
            string proticolID = identifier[0];
            string proticolIP = identifier[1];
            


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
                System.Diagnostics.Debug.WriteLine(proticolIP);
                try
                {
                    PingReply reply = pingSender.Send(proticolIP, timeout, buffer, options);



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

            int value;

            //up
            if (packetLoss < 1)
            {
                value = 1;
            }
            //unstable
            else if (1 <= packetLoss && packetLoss <= 5)
            {
                value = 2;
            }
            //down
            else
            {
                value = 0;
            }

            Dictionary<string, string> ret = new Dictionary<string, string>();
            string val = value.ToString() + "9" + weight.ToString();
            ret.Add(proticolID,val);
            queue.Enqueue(ret);
            

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

        public Thread lastthread;

        public void IncCounter()
        {
            string txt = running.Text;
            if (txt == "Tests Running: None")
            {
                running.Text = "Tests Running: 1";
            }
            else
            {
                running.Text = ("Tests Running: " + (Convert.ToInt32(txt.Substring(txt.Length - 1)) + 1).ToString());
            }
        }

        public void DecCounter()
        {
            string txt = running.Text;
            if (txt == "Tests Running: 1")
            {
                running.Text = "Tests Running: None";
            }
            else
            {
                running.Text = ("Tests Running: " + (Convert.ToInt32(txt.Substring(txt.Length - 1)) - 1).ToString());
            }
        }

        public void make_Pings()
        {
            //Declares all the neccessary elements needed
            bool pinging = true;
            List<Dictionary<string, int>> WAN = new List<Dictionary<string, int>>();
            List<Dictionary<string, int>> ESP_WAN = new List<Dictionary<string, int>>();
            List<Dictionary<string, int>> ESP_VPN = new List<Dictionary<string, int>>();
            List<Dictionary<string, int>> DNS = new List<Dictionary<string, int>>();
            List<Dictionary<string, int>> NET_DEVICES = new List<Dictionary<string, int>>();
            ConcurrentQueue<Dictionary<string, int>> PendingDevices = new ConcurrentQueue<Dictionary<string, int>>();
            ConcurrentQueue<Dictionary<string, string>> PingOut = new ConcurrentQueue<Dictionary<string, string>>();
            int endWan = 0;
            int endEspWan = 0;
            int endEspVpn = 0;
            int endDns = 0;
            int endNet = 0;
            int endDefGat = 0;
            int NetTime = 30;
            int Depth = 5;
            Dictionary<string, int> InternalScan = null;


            try
            {
                InternalScan = toolkit.js["Internal Scan"].ToObject<Dictionary<string, int>>();
                NetTime = InternalScan["ScanTime"];
                Depth = InternalScan["Network Depth"];
            }
            catch
            {
                Console.Write("No Internal Scan found in config");
                
            }

            //goes through each target and adds them to respective list
            if (this.toolkit.targets != null){
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, int>>> target in this.toolkit.targets)
                {
                    string proticol = target.Key;
                    Dictionary<string, int> Connection = target.Value["IP"];
                    foreach (KeyValuePair<string, int> ip in Connection)
                    {
                        Dictionary<string, int> address = new Dictionary<string, int>();
                        if (proticol == "WAN")
                        {
                            address.Add("a|" + ip.Key, ip.Value);
                            WAN.Add(address);
                        }
                        if (proticol == "ESP WAN")
                        {
                            address.Add("b|" + ip.Key, ip.Value);
                            ESP_WAN.Add(address);
                        }
                        if (proticol == "ESP VPN")
                        {
                            address.Add("c|" + ip.Key, ip.Value);
                            ESP_VPN.Add(address);
                        }
                        if (proticol == "DNS")
                        {
                            address.Add("d|" + ip.Key, ip.Value);
                            DNS.Add(address);
                        }
                    }
                }
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
            Dictionary<string, int> Devices = null;
            try
            {
                //Gets all network devices declared in config
                Devices = toolkit.js["Credit Unions"][toolkit.QAmap[1]]["Network Devices"].ToObject<Dictionary<string, int>>();
            }
            catch
            {
                Console.Write("No Network Devices found from CU");
            }

            if (Devices != null){
                //Adds all network devices from config to NET_DEVICES
                foreach (KeyValuePair<string, int> ip in Devices)
                {
                    if (Depth > 0)
                    {
                        Dictionary<string, int> address = new Dictionary<string, int>();
                        address.Add("e|" + ip.Key, ip.Value);
                        NET_DEVICES.Add(address);
                        Depth--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //find gateway and subnet
            string gateway = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback).SelectMany(n => n.GetIPProperties()?.GatewayAddresses).Select(g => g?.Address).Where(a => a != null).FirstOrDefault().ToString();
            string[] splitGate = gateway.Split('.');
            string subnet = splitGate[0] + "." + splitGate[1] + "." + splitGate[2] + ".";

            //TextReader ARPin = new StreamReader(@"arp.txt");
            string strARP = output;

            //adds ips from arp into NET_DEVICES until Depth is 0;
            int netitter = 0;
            
            while (InternalScan != null && Depth > 0) 
            {
                string internalIP = subnet;
                try
                {
                    internalIP = strARP.Substring(testARP(strARP, subnet, 2 + netitter), 10);
                }
                catch(Exception)
                {
                    break;
                }
                int weight = InternalScan["stage " + (netitter + 1).ToString()];
                Dictionary<string, int> address = new Dictionary<string, int>();
                address.Add("e|" + internalIP, weight);
                NET_DEVICES.Add(address);
                netitter++;
                Depth--;
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



            Dictionary<string, int> gate = new Dictionary<string, int>();
            gate.Add("f|" + gateway, 1);

            Utils.Try(() => PendingDevices.Enqueue(gate));
            Dispatcher.Invoke(IncCounter);
            Utils.Try(() => PendingDevices.Enqueue(WAN.First()));
            Dispatcher.Invoke(IncCounter);
            WAN.RemoveAt(0);
            Utils.Try(() => PendingDevices.Enqueue(ESP_WAN.First()));
            Dispatcher.Invoke(IncCounter);
            ESP_WAN.RemoveAt(0);
            Utils.Try(() => PendingDevices.Enqueue(ESP_VPN.First()));
            Dispatcher.Invoke(IncCounter);
            ESP_VPN.RemoveAt(0);
            Utils.Try(() => PendingDevices.Enqueue(DNS.First()));
            Dispatcher.Invoke(IncCounter);
            DNS.RemoveAt(0);


            ConcurrentQueue<Thread> AllThreads = new ConcurrentQueue<Thread>();

            int additionalping = 0;
            while (pinging)
            {
                if (PendingDevices.IsEmpty)
                {
                    break;
                }
                foreach (Dictionary<string, int> device in PendingDevices)
                {
                    foreach (KeyValuePair<string, int> item in device)
                    {

                        Thread thread = new Thread(() => GetNetworkStats(item.Key, 30, PingOut, item.Value));
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        AllThreads.Enqueue(thread);
                        Dictionary<string, int> other = new Dictionary<string, int>();
                        PendingDevices.TryDequeue(out other);
                    }
                }
                while (AllThreads.Count > 0) {
                    Thread t;
                    AllThreads.TryDequeue(out t);
                    if (!t.IsAlive)
                    {
                        if (PingOut.TryDequeue(out Dictionary<string, string> endping))
                        {
                            foreach (KeyValuePair<string, string> p in endping)
                            {
                                string PID = p.Key;
                                string[] retVals = p.Value.Split('9');
                                int value = Convert.ToInt32(retVals[0]);
                                int weight = Convert.ToInt32(retVals[1]);
                                string ipname;
                                if (PID == "a")
                                {
                                    ipname = "WAN";
                                }
                                else if (PID == "b")
                                {
                                    ipname = "ESP WAN";
                                }
                                else if (PID == "c")
                                {
                                    ipname = "ESP VPN";
                                }
                                else if (PID == "d")
                                {
                                    ipname = "DNS";
                                }
                                else if (PID == "e")
                                {
                                    ipname = "Network Device";
                                }
                                else if (PID == "f")
                                {
                                    ipname = "Default Gateway";
                                }
                                else
                                {
                                    ipname = "ERROR";
                                }
                                Dispatcher.Invoke(() =>
                                {
                                    Indicator ind = new Indicator();


                                    if (value == 1)
                                    {
                                        ind.colorupdate(ipname, weight);
                                        if (ipname == "WAN")
                                        {
                                            endWan = weight;
                                        }
                                        if (ipname == "ESP WAN")
                                        {
                                            endEspWan = weight;
                                        }
                                        if (ipname == "ESP VPN")
                                        {
                                            endEspVpn = weight;
                                        }
                                        if (ipname == "DNS")
                                        {
                                            endDns = weight;
                                        }
                                        if (ipname == "Network Device")
                                        {
                                            endNet = weight;
                                        }
                                        else
                                        {
                                            endDefGat = 1;
                                        }
                                    }
                                    else
                                    {
                                        additionalping++;
                                        ind.colorupdate(ipname, value);
                                        int itter = 0;
                                        if (ipname == "WAN")
                                        {
                                            endWan = value;
                                            foreach (Dictionary<string, int> item in WAN)
                                            {
                                                if (itter < additionalping)
                                                {
                                                    WAN.Remove(item);
                                                    PendingDevices.Enqueue(item);
                                                    Dispatcher.Invoke(IncCounter);
                                                    break;
                                                }
                                                itter++;
                                            }

                                        }
                                        if (ipname == "ESP WAN")
                                        {
                                            endEspWan = value;
                                            foreach (Dictionary<string, int> item in ESP_WAN)
                                            {
                                                if (itter < additionalping)
                                                {
                                                    ESP_WAN.Remove(item);
                                                    PendingDevices.Enqueue(item);
                                                    Dispatcher.Invoke(IncCounter);
                                                    break;
                                                }
                                                itter++;
                                            }
                                        }
                                        if (ipname == "ESP VPN")
                                        {
                                            endEspVpn = value;
                                            foreach (Dictionary<string, int> item in ESP_VPN)
                                            {
                                                if (itter < additionalping)
                                                {
                                                    ESP_VPN.Remove(item);
                                                    PendingDevices.Enqueue(item);
                                                    Dispatcher.Invoke(IncCounter);
                                                    break;
                                                }
                                                itter++;
                                            }
                                        }
                                        if (ipname == "DNS")
                                        {
                                            endDns = value;
                                            foreach (Dictionary<string, int> item in DNS)
                                            {
                                                if (itter < additionalping)
                                                {
                                                    DNS.Remove(item);
                                                    PendingDevices.Enqueue(item);
                                                    Dispatcher.Invoke(IncCounter);
                                                    break;
                                                }
                                                itter++;
                                            }
                                        }
                                        if (ipname == "Network Device")
                                        {
                                            endNet = value;
                                            foreach (Dictionary<string, int> item in NET_DEVICES)
                                            {
                                                if (itter < additionalping)
                                                {
                                                    NET_DEVICES.Remove(item);
                                                    PendingDevices.Enqueue(item);
                                                    Dispatcher.Invoke(IncCounter);
                                                    break;
                                                }
                                                itter++;
                                            }
                                        }
                                        else
                                        {
                                            endDefGat = value;
                                        }
                                    }
                                    DecCounter();
                                    this.Indicators.Children.Add(ind.returnGrid());

                                });
                            }
                        }
                    }
                    else
                    {
                        AllThreads.Enqueue(t);
                    }
                    
                    
                }

            }

            toolkit.NID = endWan.ToString() + endEspWan.ToString() + endEspVpn.ToString() + endDns.ToString() + endDefGat.ToString() + endNet.ToString() + sqlopen.ToString();
            this.ErrorMap += toolkit.NID;
            this.toolkit.Ernum = this.ErrorMap;
            Thread.Sleep(2000);
            Dispatcher.Invoke(() =>
            {
                InfoWin Info = new InfoWin(this.toolkit);
            
            Info.Show();
            this.Close();
            });
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
            try
            {
                toolkit.Connections();
            }
            catch
            {
                Console.Write("No Credit Union Connections Found");
            }
            
            DataContext = this;

            _bgWorker.DoWork += (s, e) =>
            {
                make_Pings();
            };

            _bgWorker.RunWorkerAsync();
        }
    }
}