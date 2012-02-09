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
				byte[] codePackage;
				int codeBuild = BuildInfo.ClientBuild;
				if (BuildInfo.CodePackageURL == null)
				{
					Console.WriteLine("[-] no client patch is available, fallback to clients compiled.code");
					var path = Settings.Default.EVEPath + "/script/compiled.code";
					if (!File.Exists(path))
					{
						Console.WriteLine("[-] failure: you have no compiled.code or your EVE path is wrong");
						Console.WriteLine("[-] aborting");
						return;
					}
					try
					{
						var common = File.ReadAllLines(Settings.Default.EVEPath + "/common.ini");
						foreach (var line in common)
						{
							if (line.StartsWith("build="))
							{
								codeBuild = int.Parse(line.Substring(6));
								Console.WriteLine("[-] (your client is on build " + codeBuild + ")");
								break;
							}
						}
					}
					catch (Exception)
					{
						Console.WriteLine("[-] failed to read build from EVE common.ini, using server info: " + codeBuild);
					}
					codePackage = File.ReadAllBytes(Settings.Default.EVEPath + "/script/compiled.code");
				}
				else
					codePackage = BuildInfo.DownloadCode(true);

                Console.WriteLine("[+] initializing cryptographic backend");
                Crypto.Initialize();
                Console.WriteLine("[+] loading compyled code into repository");
                var repo = new Repository();
                ImportLibrary(repo);
                repo.Import(new CodePackage(new MemoryStream(codePackage)));
                repo.Decompyle(codeBuild + "\\", true);
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
