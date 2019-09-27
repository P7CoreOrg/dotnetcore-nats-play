using NATS.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Requestor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Requestor().Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
    class Requestor
    {
        Dictionary<string, string> parsedArgs = new Dictionary<string, string>();

        int count = 20000;
        string url = Defaults.Url;
        string subject = "dog";
        byte[] payload = null;
        string creds = null;

        public void Run(string[] args)
        {
            Stopwatch sw = null;

            parseArgs(args);
            banner();

            Options opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = url;
            if (creds != null)
            {
                opts.SetUserCredentials(creds);
            }
           
            using (IConnection c = new ConnectionFactory().CreateConnection(opts))
            {
                sw = Stopwatch.StartNew();

                for (int i = 0; i < count; i++)
                {
                    var msg = c.Request(subject, payload,1000);
                    string s = System.Text.Encoding.UTF8.GetString(msg.Data, 0, msg.Data.Length);
                    Console.WriteLine($"Response:{s}");
                }
                c.Flush();

                sw.Stop();

                Console.Write("Completed {0} requests in {1} seconds ", count, sw.Elapsed.TotalSeconds);
                Console.WriteLine("({0} requests/second).",
                    (int)(count / sw.Elapsed.TotalSeconds));
                printStats(c);

            }
        }

        private void printStats(IConnection c)
        {
            IStatistics s = c.Stats;
            Console.WriteLine("Statistics:  ");
            Console.WriteLine("   Incoming Payload Bytes: {0}", s.InBytes);
            Console.WriteLine("   Incoming Messages: {0}", s.InMsgs);
            Console.WriteLine("   Outgoing Payload Bytes: {0}", s.OutBytes);
            Console.WriteLine("   Outgoing Messages: {0}", s.OutMsgs);
        }

        private void usage()
        {
            Console.Error.WriteLine(
                "Usage:  Requestor [-url url] [-subject subject] " +
                "[-count count] [-creds file] [-payload payload]");

            Environment.Exit(-1);
        }

        private void parseArgs(string[] args)
        {
            if (args == null)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 == args.Length)
                    usage();

                parsedArgs.Add(args[i], args[i + 1]);
                i++;
            }

            if (parsedArgs.ContainsKey("-count"))
                count = Convert.ToInt32(parsedArgs["-count"]);

            if (parsedArgs.ContainsKey("-url"))
                url = parsedArgs["-url"];

            if (parsedArgs.ContainsKey("-subject"))
                subject = parsedArgs["-subject"];

            if (parsedArgs.ContainsKey("-payload"))
                payload = Encoding.UTF8.GetBytes(parsedArgs["-payload"]);

            if (parsedArgs.ContainsKey("-creds"))
                creds = parsedArgs["-creds"];
        }

        private void banner()
        {
            Console.WriteLine("Sending {0} requests on subject {1}",
                count, subject);
            Console.WriteLine("  Url: {0}", url);
            Console.WriteLine("  Payload is {0} bytes.",
                payload != null ? payload.Length : 0);
        }

 
    }
}
