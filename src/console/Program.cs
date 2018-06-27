using System;
using System.IO;
using System.Net;
using System.Threading;

namespace eagle.tunnel.dotnet.core {
    class Program {
        public static string Version { get; } = "2.1.1";
        public static void Main (string[] args) {
            if (args.Length >= 1) {
                switch (args[0]) {
                    case "-v":
                    case "--version":
                        PrintVersion ();
                        break;
                    case "-h":
                    case "--help":
                        PrintGuide ();
                        break;
                    case "--check":
                        string confPath;
                        if (args.Length >= 2) {
                            confPath = args[1];
                        } else {
                            Console.WriteLine (
                                "warning:\tno config file input, using default /etc/eagle-tunnel.conf");
                            confPath = @"/etc/eagle-tunnel.conf";
                        }
                        CheckConfig (confPath);
                        break;
                    default:
                        if (File.Exists (args[0])) {
                            Conf.Init (args[0]);
                            Server.Start (Conf.LocalAddresses);
                        }
                        break;
                }
            } else {
                PrintGuide ();
            }
        }

        private static void PrintVersion () {
            Console.WriteLine ("Eagle Tunnel\n");
            Console.WriteLine ("UI Version: {0}", Version);
            Console.WriteLine ("Lib Version: {0}\n", Server.Version);
        }

        private static void PrintGuide () {
            Console.WriteLine ("usage: ");
            Console.WriteLine ("dotnet eagle.tunnel.dotnet.dll [option]\n");
            Console.WriteLine ("options:");
            Console.WriteLine ("[file path]\trun eagle-tunnel with specific configuration file.");
            Console.WriteLine ("-h\t--help\tshow this guide.");
            Console.WriteLine ("-v\t--version\tshow version.");
        }

        private static void CheckConfig (string confPath) {
            if (!File.Exists (confPath)) {
                Console.WriteLine ("error:\tconfig file not found: -> {0}", confPath);
                return;
            }
            Conf.Init (confPath);

            if (!Conf.allConf.ContainsKey ("listen")) {
                Console.WriteLine ("error:\tno listen");
                return;
            } else {
                if (!IPAddress.TryParse (Conf.allConf["listen"][0], out IPAddress ipa)) {
                    Console.WriteLine ("error:\tip for listen is invalid ip address");
                    return;
                }
            }

            if (!Conf.allConf.ContainsKey ("relayer")) {
                if ((Conf.allConf.ContainsKey ("http") && Conf.allConf["http"][0] == "on") ||
                    (Conf.allConf.ContainsKey ("socks") && Conf.allConf["socks"][0] == "on")) {
                    Console.WriteLine ("error:\tno relayer for http or socks");
                    return;
                }
            }

            if (!Conf.allConf.ContainsKey ("config-dir")) {
                Console.WriteLine ("error:\tno config-dir");
                return;
            }

            if (Conf.allConf.ContainsKey ("user-check") &&
                Conf.allConf["user-check"][0] == "on") {
                if (EagleTunnelUser.users.Count == 0) {
                    Console.WriteLine ("error:\tuser-check is on, but there is no user");
                }
            }

            Console.WriteLine("finished:\tno error!");
        }
    }
}