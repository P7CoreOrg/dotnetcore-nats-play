﻿using NATS.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace publish
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Publisher().Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }

        }
    }
    class Publisher
    {
        Dictionary<string, string> parsedArgs = new Dictionary<string, string>();

        int count = 1;
        string url = Defaults.Url;
        string subject = "foo";
        byte[] payload = null;
        string creds = null;
        private string user;
        private string password;

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
            
            if(user != null && password != null){
                opts.User = user;
                opts.Password = password;
            }
            using (IConnection c = new ConnectionFactory().CreateConnection(opts))
            {
                sw = Stopwatch.StartNew();

                for (int i = 0; i < count; i++)
                {
                    c.Publish(subject, payload);
                }
                c.Flush();

                sw.Stop();

                Console.Write("Published {0} msgs in {1} seconds ", count, sw.Elapsed.TotalSeconds);
                Console.WriteLine("({0} msgs/second).",
                    (int)(count / sw.Elapsed.TotalSeconds));
                printStats(c);

            }
        }

        private void printStats(IConnection c)
        {
            IStatistics s = c.Stats;
            Console.WriteLine("Statistics:  ");
            Console.WriteLine("   Outgoing Payload Bytes: {0}", s.OutBytes);
            Console.WriteLine("   Outgoing Messages: {0}", s.OutMsgs);
        }

        private void usage()
        {
            Console.Error.WriteLine(
                "Usage:  Publish [-url url] [-subject subject] " +
                "-count [count] -creds [file] [-payload payload]");

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
            if (parsedArgs.ContainsKey("-user"))
                user = parsedArgs["-user"];
            if (parsedArgs.ContainsKey("-password"))
                password = parsedArgs["-password"];
        }

        private void banner()
        {
            Console.WriteLine("Publishing {0} messages on subject {1}",
                count, subject);
            Console.WriteLine("  Url: {0}", url);
            Console.WriteLine("  Subject: {0}", subject);
            Console.WriteLine("  Count: {0}", count);
            Console.WriteLine("  Payload is {0} bytes.",
                payload != null ? payload.Length : 0);
        }
    }
}
