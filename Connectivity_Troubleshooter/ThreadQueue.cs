using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connectivity_Troubleshooter
{
    class ThreadQueue
    {
        ConcurrentQueue<Dictionary<string,string>> hosts = new ConcurrentQueue<Dictionary<string, string>>();

        public void AddHost(Dictionary<string, string> host)
        {
            hosts.Enqueue(host);
        }

        public void AddHost(string addr, string name)
        {
            Dictionary<string, string> host = new Dictionary<string, string>();
            host.Add(addr, name);
            hosts.Enqueue(host);
        }
    }
}
