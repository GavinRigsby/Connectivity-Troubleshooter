using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Connectivity_Troubleshooter
{
    class logger
    {
        public string[] logs;
        public string gateway;
        public logger(Json_tool toolkit)
        {
            Dictionary<string, string> LogOptions = toolkit.js["Logs"].ToObject<Dictionary<string, string>>();
            string IP = "", OS = "", name = "", email = "", CU = "", Error = "", defaultgate = "", arpReturn = "";
            bool ReturnIp = false, ReturnOS = false, ReturnEmail = false, ReturnCU = false, ReturnError = false, BothTime = false, Localtime = false, UTCtime = false, ReturnGateway = true, ReturnARP = true;
            foreach (KeyValuePair<string,string> option in LogOptions)
            {
                if (option.Value == "True")
                {
                    if (option.Key == "ReturnIP"){
                        var host = Dns.GetHostEntry(Dns.GetHostName());
                        foreach (var ip in host.AddressList)
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork)
                            {
                                IP = ip.ToString();
                                ReturnIp = true;
                                break;
                            }
                        }
                        if (IP == "")
                        {
                            throw new Exception("No network adapters with an IPv4 address in the system!");
                        }
                    }
                    if (option.Key == "ReturnOS")
                    {
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                        {
                            ManagementObjectCollection information = searcher.Get();
                            if (information != null)
                            {
                                foreach (ManagementObject obj in information)
                                {
                                    OS = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                                }
                            }
                            OS = OS.Replace("NT 5.1.2600", "XP");
                            OS = OS.Replace("NT 5.2.3790", "Server 2003");
                            ReturnOS = true;
                        }
                    }
                    if (option.Key == "ReturnName")
                    {
                        if (toolkit.QAmap[2] == ""){
                            name = "user";
                        }
                        else {
                            name = toolkit.QAmap[2];
                        }
                    }
                    if (option.Key == "ReturnEmail")
                    {
                        if (toolkit.QAmap[3] == "")
                        {
                            email = "unknown";
                        }
                        else
                        {
                            email = toolkit.QAmap[2];
                        }
                        ReturnEmail = true;
                    }
                    if (option.Key == "ReturnCU")
                    {
                        CU = toolkit.QAmap[1];
                        ReturnCU = true;
                    }
                    if (option.Key == "ReturnError")
                    {
                        Error =  toolkit.Ernum;
                        ReturnError = true;
                    }
                    DateTime Utctime = DateTime.UtcNow;
                    DateTime localtime = DateTime.Now;
                    string[] log = { Error, localtime.ToString(), Utctime.ToString(), IP, OS, CU, name, email };
                    this.logs = log;
                }
            }
            using (StreamWriter writer = new StreamWriter(@"out.log"))
            {
                string logfor = "", timing = "", sysinfo = "", errorinfo = "", outlog = "";
                //userlogging
                if (ReturnEmail){
                    logfor = "This log is for " + this.logs[6] + "(" + this.logs[7] + ") at " + this.logs[5] +"\n";  
                }
                else
                {
                    logfor = "This log is for " + this.logs[6] + " at " + this.logs[5] + "\n";
                }

                //Time Logging
                if (BothTime || (UTCtime && Localtime))
                {
                    timing = "Log taken place " + this.logs[1] + " Local time and " + this.logs[2] + "UTC \n";
                }
                else if (UTCtime || Localtime)
                {
                    if (Localtime)
                    {
                        timing = "Log taken place " + this.logs[1] + " Local time \n";
                    }
                    else
                    {
                        timing = "Log taken place " + this.logs[2] + "UTC \n";
                    }
                }

                //System Info logging
                if (ReturnOS && ReturnIp)
                {
                    sysinfo = "System IP: " + this.logs[3] + "\tSystem OS: " + this.logs[4] + "\n" ;
                }
                else if (ReturnOS || ReturnIp)
                {
                    if (ReturnOS)
                    {
                        sysinfo = "System OS: " + this.logs[4] + "\n";
                    }
                    else
                    {
                        sysinfo = "System IP: " + this.logs[3] + "\n";
                    }
                }

                if (ReturnError)
                {
                    errorinfo = "Error code of (" + this.logs[0] + ") with error description of " + toolkit.ErDisplay(toolkit.Ernum) + "\n"; 
                }

                if (ReturnGateway)
                {
                    string gateway = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback).SelectMany(n => n.GetIPProperties()?.GatewayAddresses).Select(g =>g?.Address).Where(a =>a != null).FirstOrDefault().ToString();
                    defaultgate = "The default gateway to the computer is " + gateway + "\n";
                    this.gateway = gateway;
                }
                if (ReturnARP)
                {
                    arpReturn = "Check the arp.out file in the same directory to look at arp table \n";
                }
               
                outlog = logfor + timing + sysinfo + errorinfo + defaultgate + arpReturn;
                writer.WriteLine(outlog);
            }
        }
    }
}
