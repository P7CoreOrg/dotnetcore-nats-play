using NATS.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Utils;

namespace Subscribe
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Subscriber().Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
    class Subscriber
    {
        Dictionary<string, string> parsedArgs = new Dictionary<string, string>();

        int count = 1000000;
        string url = Defaults.Url;
        string subject = "foo";
        bool sync = false;
        int received = 0;
        bool verbose = false;
        string creds = null;
        string queueGroup = null;
        private string user;
        private string password;

        public void Run(string[] args)
        {
            parseArgs(args);
            banner();

            Options opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = url;
            opts.ReconnectWait = 1;
            opts.AllowReconnect = true;
            // PingInterval is connected to reconnect.
            opts.PingInterval = 1000;// this defaults to 2 minutes which sould be ok in production.  We don't want a swarm of subscribers killing the nats server.


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
                TimeSpan elapsed;

                if (sync)
                {
                    elapsed = receiveSyncSubscriber(c);
                }
                else
                {
                    elapsed = receiveAsyncSubscriber(c);
                }

                Console.Write("Received {0} msgs in {1} seconds ", received, elapsed.TotalSeconds);
                Console.WriteLine("({0} msgs/second).",
                    (int)(received / elapsed.TotalSeconds));
                printStats(c);

            }
        }

        private void printStats(IConnection c)
        {
            IStatistics s = c.Stats;
            Console.WriteLine("Statistics:  ");
            Console.WriteLine("   Incoming Payload Bytes: {0}", s.InBytes);
            Console.WriteLine("   Incoming Messages: {0}", s.InMsgs);
        }

        private TimeSpan receiveAsyncSubscriber(IConnection c)
        {
            Stopwatch sw = new Stopwatch();
            Object testLock = new Object();

            EventHandler<MsgHandlerEventArgs> msgHandler = (sender, args) =>
            {
                if (received == 0)
                    sw.Start();

                received++;

                if (verbose)
                    Console.WriteLine("Received: " + args.Message);

                if (received >= count)
                {
                    sw.Stop();
                    lock (testLock)
                    {
                        Monitor.Pulse(testLock);
                    }
                }
            };

            using (IAsyncSubscription s = (
                queueGroup == null?
                c.SubscribeAsync(subject, msgHandler): 
                c.SubscribeAsync(subject, queueGroup, msgHandler)))
            {
                // just wait until we are done.
                lock (testLock)
                {
                    Monitor.Wait(testLock);
                }
            }

            return sw.Elapsed;
        }


        private TimeSpan receiveSyncSubscriber(IConnection c)
        {
            using (ISyncSubscription s = (
                queueGroup == null?
                c.SubscribeSync(subject): 
                c.SubscribeSync(subject, queueGroup)))
            {
                Stopwatch sw = new Stopwatch();

                while (received < count)
                {
                    if (received == 0)
                        sw.Start();

                    Msg m = s.NextMessage();
                    received++;

                    if (verbose)
                        Console.WriteLine("Received: " + m);
                }

                sw.Stop();

                return sw.Elapsed;
            }
        }

        private void usage()
        {
            Console.Error.WriteLine(
                "Usage:  Subscribe [-url url] [-subject subject] " +
                "[-count count] [-creds file] [-sync] [-verbose]");

            Environment.Exit(-1);
        }

        private void parseArgs(string[] args)
        {
            if (args == null)
                return;
            bool exists = false;
            (exists, user) = "USER".GetEnvironmentVariable(null);
            (exists, password) = "PASSWORD".GetEnvironmentVariable(null);
           
            (exists, verbose) = "VERBOSE".GetEnvironmentVariable(false);
            (exists, subject) = "SUBJECT".GetEnvironmentVariable(subject);
            (exists, url) = "URL".GetEnvironmentVariable(url);
            (exists, queueGroup) = "QUEUE_GROUP".GetEnvironmentVariable(queueGroup);

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-sync") ||
                    args[i].Equals("-verbose"))
                {
                    parsedArgs.Add(args[i], "true");
                }
                else
                {
                    if (i + 1 == args.Length)
                        usage();

                    parsedArgs.Add(args[i], args[i + 1]);
                    i++;
                }

            }

            if (parsedArgs.ContainsKey("-count"))
                count = Convert.ToInt32(parsedArgs["-count"]);

            if (parsedArgs.ContainsKey("-url"))
                url = parsedArgs["-url"];
            if (parsedArgs.ContainsKey("-user"))
                user = parsedArgs["-user"];
            if (parsedArgs.ContainsKey("-password"))
                password = parsedArgs["-password"];
            if (parsedArgs.ContainsKey("-subject"))
                subject = parsedArgs["-subject"];

            if (parsedArgs.ContainsKey("-sync"))
                sync = true;

            if (parsedArgs.ContainsKey("-verbose"))
                verbose = true;

            if (parsedArgs.ContainsKey("-creds"))
                creds = parsedArgs["-creds"];

            Console.WriteLine($"VERBOSE={verbose}");
            Console.WriteLine($"SUBJECT={subject}");
            Console.WriteLine($"URL={url}");
            Console.WriteLine($"QUEUE_GROUP={queueGroup}");
            Console.WriteLine($"USER={user}");
            Console.WriteLine($"PASSWORD={password}");
        }

        private void banner()
        {
            Console.WriteLine("Receiving {0} messages on subject {1}",
                count, subject);
            Console.WriteLine("  Url: {0}", url);
            Console.WriteLine("  Subject: {0}", subject);
            Console.WriteLine("  Receiving: {0}",
                sync ? "Synchronously" : "Asynchronously");
        }

         
    }
}
