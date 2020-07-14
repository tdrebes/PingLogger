using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingLogger
{
    class PingModule
    {
        public static string ip = "1.1.1.1";
        public static int interval = 1000;
        public static int timeout = 500;
        public static bool onlyLogTimeouts = false;



        public static long GetPing(string ip) {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;


            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            PingReply reply = pingSender.Send(ip, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                Debug.WriteLine("Address: {0}", reply.Address.ToString());
                Debug.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Debug.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Debug.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Debug.WriteLine("Buffer size: {0}", reply.Buffer.Length);
            }


            return reply.RoundtripTime;
        }

        public static void logPingTask()
        {
            int n = 0;
            while (true)
            {
                using (Ping p = new Ping())
                {
                    long ping = p.Send(ip).RoundtripTime;
                    double value = 0;

                    if (ping == 0)
                    {
                        value = double.NaN;
                        Debug.WriteLine("No Connection... ");
                        Debug.WriteLine(ping);
                        MainWindow.dbConnector.InsertPing(ping);
                        MainWindow.dbConnector.InsertTimeout();

                        //value = 999;
                    }
                    else
                    {
                        value = ping;
                        if(!onlyLogTimeouts) MainWindow.dbConnector.InsertPing(ping);

                    }
                    //MainWindow.graph.Values.Add(new ObservableValue(value));
                    MainWindow.graph.Values.Add(new ObservableValue(value));
                    if (n >= 20)
                    {
                        MainWindow.graph.Values.RemoveAt(0);
                    }
                    n++;

                }


                Thread.Sleep(interval);
            }
        }


    }

}
