using System;
using System.Collections.Generic;
using System.Net;
using Dem0n13.SocketServer;

namespace Dem0n13.ServerApplication
{
    internal class ConsoleApplication
    {
        private const int Port = 50000;
        
        private static void Main()
        {
            Console.WriteLine("Server application. Please, choose the ip address:");
            
            // print all available interfaces
            var ips = GetAvailibaleIPs();
            foreach (var ip in ips)
            {
                Console.WriteLine(ip.Item2);
            }

            int choose;
            var serverIP = ips[0].Item1; // defaults - 127.0.0.1

            Console.Write("> ");
            if (int.TryParse(Console.ReadLine(), out choose))
            {
                if (0 <= choose && choose < ips.Count)
                {
                    serverIP = ips[choose].Item1; // ip user's choice
                }
            }

            // create the server
            var server = new UdpSocketServer<MockLogicServer, NullLogicServer>(serverIP, Port, 1024, 10);

            Console.WriteLine("Control panel:");
            Console.WriteLine("1: Start");
            Console.WriteLine("2: Stop");
            Console.WriteLine("3: Restart");
            Console.WriteLine("0: Stop and exit");
            Console.WriteLine("Default: get response from logic server");

            string input;
            do
            {
                Console.Write("> ");
                input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        server.Start();
                        break;
                    case "0":
                    case "2":
                        server.Stop();
                        break;
                    case "3":
                        server.Restart();
                        break;
                    default:
                        var response = server.LogicServer.GetResponse(input);
                        Console.WriteLine("< " + response);
                        break;
                }
            } while (!string.Equals(input, "0"));
        }

        private static List<Tuple<IPAddress, string>> GetAvailibaleIPs()
        {
            var result = new List<Tuple<IPAddress, string>>();

            var i = 0;
            result.Add(new Tuple<IPAddress, string>(IPAddress.Loopback, i++ + ". localhost (default)"));
            result.Add(new Tuple<IPAddress, string>(IPAddress.Any, i++ + ". All interfaces"));

            foreach (var ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                result.Add(new Tuple<IPAddress, string>(ipAddress, i++ + ". " + ipAddress));
            }

            return result;
        }
    }
}
