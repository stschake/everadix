using System;
using System.IO;
using everadix.Properties;

namespace everadix
{
    class Program
    {
        static void Main()
        {
            try
            {
                Decompyler.VerifySetup();
                BuildInfo.Update(BuildInfo.SelectByHighestBuild(true));
                var codePackage = BuildInfo.DownloadCode(true);
                Console.WriteLine("[+] initializing cryptographic backend");
                Crypto.Initialize();
                Console.WriteLine("[+] loading compyled code into repository");
                var repo = new Repository();
                ImportLibrary(repo);
                repo.Import(new CodePackage(new MemoryStream(codePackage)));
                repo.Decompyle(BuildInfo.ClientBuild + "\\", true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Exception: " + ex);
                Console.ReadLine();
            }
        }

        static void ImportLibrary(Repository repo)
        {
            if (!Directory.Exists(Settings.Default.EVEPath))
            {
                Console.WriteLine("[-] not decompyling library: can't find EVE directory");
                return;
            }

            var path = Settings.Default.EVEPath + "/lib/";
            if (!Directory.Exists(path))
            {
                Console.WriteLine("[-] not decompyling library: can't find lib directory");
                return;
            }

            foreach (var zip in new []{"carbonlib.ccp", "carbonstdlib.ccp", "evelib.ccp"})
            {
                var zippath = path + zip;
                if (!File.Exists(zippath))
                {
                    Console.WriteLine("[-] not importing " + zip + ": file not found");
                    continue;
                }
                try
                {
                    repo.Import(new CodeZip(zip.Substring(0, zip.IndexOf('.')), File.OpenRead(zippath)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] not importing " + zip + ": " + ex.Message);
                    continue;
                }

                Console.WriteLine("[+] imported " + zip + " library");
            }
        }
    }
}
