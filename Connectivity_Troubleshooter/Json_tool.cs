using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Web;
using System.Windows.Controls;
using System.Windows;
using System.Linq;

namespace Connectivity_Troubleshooter
{
    public class Json_tool
    {
        //instantiate variables used in Json_tool
        public dynamic js;
        public dynamic map;
        public string count;
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> targets;
        public string[] QAmap
        { get; set; }
        public string QAName;
        public string QAProb;
        public string ID;
        public string Ernum
        { get; set; }
        public string Error;
        public string Diagnosis;
        public List<string> steps = new List<string>();
        public List<string> desc = new List<string>();
        public string contacts = "";
        public string CUID;
        public string NID;
        public void start()
        {
            this.js = ConvertJsonFile();
        }


        //Takes text from config.json and reads globals
        //from json replacing them with the wanted replacements
        //returns a dynamic Json and sets to 
        public dynamic ConvertJsonFile()
        {
            TextReader tr = new StreamReader(@"config.json");
            string strJson = tr.ReadToEnd();
            var jConf = JsonConvert.DeserializeObject<dynamic>(strJson);

            string CU = this.QAmap[1];
            try
            {
                Dictionary<string, Dictionary<string, dynamic>> CUJson = jConf["Credit Unions"].ToObject<Dictionary<string, Dictionary<string, dynamic>>>();
                Dictionary<string, string> CUcontacts = jConf["Credit Unions"][CU]["Contacts"].ToObject<Dictionary<string, string>>();
                this.ID = CUJson[CU]["ID"][0].Value;
                foreach (KeyValuePair<string, string> contact in CUcontacts)
                {
                    this.contacts += contact.Key + ": " + contact.Value + ",";
                }
                
            }
            catch
            {
                Console.Write("No Credit Union Contacts Found");
                this.ID = "NONE";
            }
            try
            {
                Dictionary<string, string> Globalcontacts = jConf["Contacts"].ToObject<Dictionary<string, string>>();
                foreach (KeyValuePair<string, string> contact in Globalcontacts)
                {
                    if (!this.contacts.Contains(contact.Key))
                    {
                        this.contacts += contact.Key + ": " + contact.Value + ",";
                    }
                }
            }
            catch
            {
                this.contacts += "None: Found";
            }
            


            dynamic Json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(strJson);
            return Json;
        }


        //Given the error code it checks if there is a corrisponding diagnosis
        //also checks for diagnosis' with wildcards "3" representing any value
        public string ErDisplay(string ErInput)
        {
            try
            {
                if (this.js["ErrorMap"][ErInput] != null)
                {
                    this.Diagnosis = ReturnD(ErInput);
                    return this.Diagnosis;
                }
                else
                {
                    //checks if there is a diagnosis that uses a wildcard that works
                    for (int i = 0; i < 7; i++)
                    {
                        char[] ch = ErInput.ToCharArray();
                        ch[i] = '3';
                        string newStr = new string(ch);
                        if (this.js["ErrorMap"][newStr] != null)
                        {
                            this.Diagnosis = ReturnD(newStr);
                            return this.Diagnosis;
                        }
                    }
                }
            }
            catch
            {
                Console.Write("No Map for Diagnosis");
            }
            
            this.Diagnosis = "Unknown Error Code";
            return this.Diagnosis;
        }

        //returns the diagnosis text from the config and appends together with & for better readablility
        public string ReturnD(string Erinput)
        {
            string first = this.js["ErrorMap"][Erinput][0];
            string s = "";
            foreach (string item in this.js["ErrorMap"][Erinput])
            {
                if (item == first)
                {
                    s += item;
                }
                else
                {
                    s += " & " + item;
                }

            }
            return s;

        }

        //maps Targets to dictionary object and sets Json_tool variable
        public void Connections()
        {

            Dictionary<string, Dictionary<string, Dictionary<string, int>>> GlobTargets = this.js["Targets"].ToObject<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> Targets = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            bool available = true;
            try { 
                Dictionary<string, Dictionary<string, Dictionary<string, int>>> CUTargets = this.js["Credit Unions"][this.QAmap[1]]["Targets"].ToObject<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>();
            }
            catch
            {
                available = false;
            }
            if (available)
            {
                Dictionary<string, Dictionary<string, Dictionary<string, int>>> CUTargets = this.js["Credit Unions"][this.QAmap[1]]["Targets"].ToObject<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>();
                string[] tars = { "WAN", "ESP WAN", "ESP VPN", "DNS" };
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, int>>> item in GlobTargets)
                {
                    int index = Array.IndexOf(tars, item.Key);
                    CUTargets.TryGetValue(tars[index], out Dictionary<string, Dictionary<string, int>> newvals);
                    newvals.TryGetValue("IP", out Dictionary<string, int> CUIP);
                    List<KeyValuePair<string, int>> CUList = CUIP.ToList<KeyValuePair<string, int>>();
                    item.Value.TryGetValue("IP", out Dictionary<string, int> GLIP);
                    List<KeyValuePair<string, int>> GLList = GLIP.ToList<KeyValuePair<string, int>>();
                    Dictionary<string, Dictionary<string, int>> NewIP = new Dictionary<string, Dictionary<string, int>>();
                    Dictionary<string, int> Addresses = new Dictionary<string, int>();
                    for (int i = 0; i < CUList.Count(); i++)
                    {
                        Addresses.Add(CUList[i].Key, CUList[i].Value);
                    }
                    for (int i = 0; i < GLList.Count(); i++)
                    {
                        Addresses.Add(GLList[i].Key, GLList[i].Value);
                    }
                    NewIP.Add("IP", Addresses);
                    Targets[tars[index]] = NewIP;
                    Console.Write("YESS");
                }
            }
            this.targets = Targets;
        }

        public bool QAMap()
        {
            try
            {
                Dictionary<string, Dictionary<string, string[]>> qa = this.js["QAMAP"].ToObject<Dictionary<string, Dictionary<string, string[]>>>();
                string problems = "";
                int probCount = 1;
                foreach (KeyValuePair<string, Dictionary<string, string[]>> code in qa)
                {
                    if (this.QAmap[0] == code.Key)
                    {

                        foreach (KeyValuePair<string, string[]> problem in code.Value)
                        {
                            this.QAName = problem.Key;
                            foreach (string item in problem.Value)
                            {
                                problems += probCount + ". " + item + "\n";
                                probCount += 1;
                            }
                        }
                        this.QAProb = problems;
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                Console.Write("Could not find QAMAP in config");
                return false;
            }
        }


        /*public List<string> PopulateCU()
        {
            TextReader tr = new StreamReader(@"config.json");
            string strJson = tr.ReadToEnd();
            var json = JsonConvert.DeserializeObject<dynamic>(strJson);
            Dictionary<string, dynamic> CreditUnions = json["Credit Unions"].ToObject<Dictionary<string, dynamic>>();
            List<string> Unions = new List<string>();
            foreach (KeyValuePair<string,dynamic> Union in CreditUnions)
            {
                Unions.Add(Union.Key);
            }
            return Unions;
        }*/

        public object PopulateCU(MainWindow sender)
        {
            TextReader tr = new StreamReader(@"config.json");
            string strJson = tr.ReadToEnd();
            var json = JsonConvert.DeserializeObject<dynamic>(strJson);
            try
            {
                Dictionary<string, dynamic> CreditUnions = json["Credit Unions"].ToObject<Dictionary<string, dynamic>>();
                List<string> Unions = new List<string>();
                foreach (KeyValuePair<string, dynamic> Union in CreditUnions)
                {
                    sender.CU.Items.Add(Union.Key);
                }
                return sender.CU;
            }
            catch
            {
                Console.Write("No Unions in Config using Textbox");
                sender.Stack.Children.Remove(sender.CU);
                TextBox tb = new TextBox();
                tb.Name = "CU";
                tb.Margin = new Thickness(25, 10, 10, 10);
                tb.Padding = new Thickness(5);
                sender.Stack.Children.Insert(1, tb);
                return tb;
            }
        }



        //maps suggestion map to dictionary and pulls the description, steps, and contacts for each
        //mulitple diagnosis' will give multiple steps
        //contacts do not have duplicates when multiple diagnosis' are given
        public void DisplaySteps()
        {
            try
            {
                Dictionary<string, Dictionary<string, string[]>> Suggestion = this.js["SuggestMap"].ToObject<Dictionary<string, Dictionary<string, string[]>>>();
                foreach (KeyValuePair<string, Dictionary<string, string[]>> diagnosis in Suggestion)
                {

                    //splits the diagnosis' into string array
                    string[] D = this.Diagnosis.ToUpper().Split('&');
                    //removes unneccisarry whitespace at end for searching config
                    foreach (string item in D)
                    {
                        D[Array.IndexOf(D, item)] = item.Trim();
                    }
                    //searches through config for matching diagnosis
                    if (Array.IndexOf(D, diagnosis.Key) > -1)
                    {
                        foreach (KeyValuePair<string, string[]> steps in diagnosis.Value)
                        {
                            //print diagnosis descripition
                            if (steps.Key == "Description")
                            {
                                string[] desc = (String[])steps.Value;
                                this.desc.Add(diagnosis.Key + ": " + desc[0] + "\n");
                            }
                            //print diagnosis steps
                            else if (steps.Key == "Steps")
                            {
                                int stepcounter = 1;
                                foreach (string step in steps.Value)
                                {
                                    this.steps.Add("\t" + stepcounter.ToString() + ". " + step + "\n");
                                    stepcounter += 1;
                                }
                            }
                            //print diagnosis contacts
                            else if (steps.Key == "ContactList")
                            {
                                //contact is string global
                                string contactlist = "";
                                Dictionary<string, string> Cucontacts = this.js["Credit Unions"][this.QAmap[1]]["Contacts"].ToObject<Dictionary<string, string>>();
                                Dictionary<string, string> Globalcontacts = this.js["Contacts"].ToObject<Dictionary<string, string>>();
                                foreach (string contact in steps.Value)
                                {
                                    string thiscontact = contact.Replace("$", "");
                                    if (this.contacts.Contains(contact.Replace("$", "")))
                                    {
                                        string number = Globalcontacts[thiscontact];
                                        if (Cucontacts.ContainsKey(thiscontact))
                                        {
                                            number = Cucontacts[thiscontact];
                                        }

                                        contactlist += thiscontact + ": " + number;
                                    }
                                    else
                                    {
                                        contactlist += thiscontact + ": [Contact Not Found] ";
                                    }
                                    if (contact != steps.Value[steps.Value.Length - 1])
                                    {
                                        contactlist += " , ";
                                    }

                                }
                                this.contacts = contactlist;

                            }
                        }
                    }
                }
            }
            catch
            {
                Console.Write("NO CONFIG FOUND FOR STEPS");
            }
            
        }
    }
}