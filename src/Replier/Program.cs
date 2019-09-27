using NATS.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Utils;

namespace Replier
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Replier().Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
    class Replier
    {
        Dictionary<string, string> parsedArgs = new Dictionary<string, string>();

        int count = 20000;
        string url = Defaults.Url;
        //string url = "nats://nats:4222";
        string subject = "foo";
        bool sync = false;
        int received = 0;
        bool verbose = false;
        Msg replyMsg = new Msg();
        string creds = null;
        private string queueGroup=null;

        public void Run(string[] args)
        {
            parseArgs(args);
            banner();

            Options opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = url;
            if (creds != null)
            {
                opts.SetUserCredentials(creds);
            }

            replyMsg.Data = Encoding.UTF8.GetBytes("reply");

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

                Console.Write("Replied to {0} msgs in {1} seconds ", received, elapsed.TotalSeconds);
                Console.WriteLine("({0} replies/second).",
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
            Console.WriteLine("   Outgoing Payload Bytes: {0}", s.OutBytes);
            Console.WriteLine("   Outgoing Messages: {0}", s.OutMsgs);
        }

        private TimeSpan receiveAsyncSubscriber(IConnection c)
        {
            Stopwatch sw = null;
            AutoResetEvent subDone = new AutoResetEvent(false);

            EventHandler<MsgHandlerEventArgs> msgHandler = (sender, args) =>
            {
                if (received == 0)
                {
                    sw = new Stopwatch();
                    sw.Start();
                }

                received++;

                if (verbose)
                    Console.WriteLine("Received: " + args.Message);

                replyMsg.Subject = args.Message.Reply;
                c.Publish(replyMsg);
                c.Flush();

                if (received == count)
                {
                    sw.Stop();
                    subDone.Set();
                }
            };

            using (IAsyncSubscription s = (queueGroup == null?c.SubscribeAsync(subject, msgHandler): c.SubscribeAsync(subject, queueGroup,msgHandler)))
            {
                // just wait to complete
                subDone.WaitOne();
            }

            return sw.Elapsed;
        }


        private TimeSpan receiveSyncSubscriber(IConnection c)
        {
            using (ISyncSubscription s = queueGroup==null?c.SubscribeSync(subject): c.SubscribeSync(subject, queueGroup))
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

                    replyMsg.Subject = m.Reply;
                    c.Publish(replyMsg);
                }

                sw.Stop();

                return sw.Elapsed;
            }
        }

        private void usage()
        {
            Console.Error.WriteLine(
                "Usage:  Replier [-url url] [-subject subject] " +
                "-count [count] -creds [file] [-sync] [-verbose]");

            Environment.Exit(-1);
        }

        private void parseArgs(string[] args)
        {
            if (args == null)
                return;
            bool exists = false;
            (exists,verbose) = "VERBOSE".GetEnvironmentVariable(false);
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
        }

        private void banner()
        {
            Console.WriteLine("Receiving {0} messages on subject {1}",
                count, subject);
            Console.WriteLine("  Url: {0}", url);
            Console.WriteLine("  Receiving: {0}",
                sync ? "Synchronously" : "Asynchronously");
        }
 
    }
}
