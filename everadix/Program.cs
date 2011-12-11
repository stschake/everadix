using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace everadix
{
    class Program
    {
        static void Main()
        {
            BuildInfo.Update(BuildInfo.SelectByHighestBuild(true));
            var codePackage = BuildInfo.DownloadCode(true);

            Console.WriteLine("[+] initializing cryptographic backend");
            Crypto.Initialize();
            Console.WriteLine("[+] loading compyled code into repository");
            var repo = new Repository();
            //repo.Import(new CodeZip("evelib", File.OpenRead("lib\\evelib.ccp")));
            //repo.Import(new CodeZip("carbonlib", File.OpenRead("lib\\carbonlib.ccp")));
            //repo.Import(new CodeZip("carbonstdlib", File.OpenRead("lib\\carbonstdlib.ccp")));
            repo.Import(new CodePackage(new MemoryStream(codePackage)));
            repo.Decompyle(BuildInfo.ClientBuild + "\\", true);
        }
    }
}
