using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using eveMarshal;

namespace everadix
{

    public enum EVEServer
    {
        Tranquility,
        Singularity,
        Multiplicity,
        Duality,
        Chaos
    }

    public static class BuildInfo
    {
        private static readonly Dictionary<EVEServer, string> ServerHosts = new Dictionary<EVEServer, string>
                                                                                {
                                                                                    {EVEServer.Tranquility, "87.237.38.200"},
                                                                                    {EVEServer.Singularity, "87.237.38.50"},
                                                                                    {EVEServer.Multiplicity, "87.237.38.51"},
                                                                                    {EVEServer.Duality, "87.237.38.60"},
                                                                                    {EVEServer.Chaos, "87.237.38.55"}
                                                                                };

        private static ManualResetEventSlim _downloadComplete;
        private static byte[] _downloadResult;
        private static int _oldPercentage;

        public static int ServerBuild { get; private set; }
        public static int ClientBuild { get; private set; }
        public static string CodePackageURL { get; private set; }

        public static void Update(EVEServer server)
        {
            int serverBuild, clientBuild;
            string codePackageURL;
            UpdateInternal(server, out serverBuild, out clientBuild, out codePackageURL);
            ServerBuild = serverBuild;
            ClientBuild = clientBuild;
            CodePackageURL = codePackageURL;
        }

        public static EVEServer SelectByHighestBuild(bool verbose = false)
        {
            EVEServer highestServer = EVEServer.Tranquility;
            int highestBuild = 0;
            foreach (var server in Enum.GetValues(typeof(EVEServer)))
            {
                int serverBuild, clientBuild;
                string codePackageURL;
                if (verbose)
                    Console.Write("[+] polling " + server + ".. ");
                if (!UpdateInternal((EVEServer)server, out serverBuild, out clientBuild, out codePackageURL))
                {
                    if (verbose)
                        Console.WriteLine("not reachable");
                }
                else
                {
                    if (verbose)
                        Console.WriteLine(clientBuild.ToString());
                    if (clientBuild > highestBuild)
                    {
                        highestServer = (EVEServer)server;
                        highestBuild = clientBuild;
                    }
                }
            }
            if (verbose)
                Console.WriteLine("[+] " + highestServer + " has highest build (" + highestBuild + ")");
            return highestServer;
        }

        private static bool UpdateInternal(EVEServer server, out int serverBuild, out int clientBuild, out string codePackageURL)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                socket.Connect(ServerHosts[server], 26000);
                var reader = new BinaryReader(new NetworkStream(socket));
                var len = reader.ReadInt32();
                var data = reader.ReadBytes(len);
                var hello = Unmarshal.Process<eveMarshal.PyTuple>(data);
                serverBuild = (int)hello[4].IntValue;
                if (hello[6] is PyNone)
                {
                    // no client patch available
                    clientBuild = serverBuild;
                    codePackageURL = null;
                }
                else
                {
                    var updateInfo = (hello[6] as PyObjectData).Arguments as eveMarshal.PyDict;
                    clientBuild = (int)updateInfo.Get("build").IntValue;
                    codePackageURL = updateInfo.Get("fileurl").StringValue;
                }
                socket.Close();
                return true;
            }
            catch (Exception e)
            {
                serverBuild = clientBuild = 0;
                codePackageURL = null;
                return false;
            }
        }

        /// <summary>
        /// Downloads the current compiled.code package
        /// </summary>
        /// <returns>the raw compiled.code data</returns>
        public static byte[] DownloadCode(bool verbose = false)
        {
            if (verbose)
                Console.WriteLine("[+] initiating download of build");
            var wc = new WebClient();

            // skip the extra work for progress display
            if (!verbose)
                return wc.DownloadData(CodePackageURL);

            _downloadComplete = new ManualResetEventSlim(false);
            wc.DownloadProgressChanged += HandleDownloadProgress;
            wc.DownloadDataCompleted += HandleDownloadCompleted;
            Console.Write("[+] loading.. ");
            wc.DownloadDataAsync(new Uri(CodePackageURL));
            _downloadComplete.Wait();
            Console.WriteLine();
            Console.WriteLine("[+] download complete");
            return _downloadResult;
        }

        private static void HandleDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            _downloadResult = e.Result;
            _downloadComplete.Set();
        }

        private static void HandleDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            if (_oldPercentage == e.ProgressPercentage)
                return;
            _oldPercentage = e.ProgressPercentage;
            if (e.ProgressPercentage % 10 == 0)
                Console.Write(e.ProgressPercentage + "% ");
        }
    }

}