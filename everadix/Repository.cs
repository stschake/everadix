using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace everadix
{

    public class Repository
    {
        public List<CodeFile> Files = new List<CodeFile>(1500);

        public void Import(IEnumerable<CodeFile> source)
        {
            Files.AddRange(source);
        }

        public void Decompyle(string outputDir, bool verbose = false)
        {
            if (verbose)
                Console.WriteLine("[+] cleaning up the output directory..");
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);

            if (verbose)
                Console.WriteLine("[+] decompyling " + Files.Count + " files in repository");
            int i = 0;
            foreach (var file in Files)
            {
                if (verbose)
                    Console.Write("[+] decompyling " + Path.GetFileNameWithoutExtension(file.Path) + ".. ");
                var sw = new Stopwatch();
                sw.Start();
                var data = Decompyler.Decompyle(file.Data, file.MissingHeader);
                sw.Stop();
                if (data == null)
                {
                    if (verbose)
                        Console.WriteLine("failed");
                }
                else
                {
                    if (verbose)
                        Console.WriteLine(sw.ElapsedMilliseconds + "ms");
                    var outPath = outputDir + file.Path;
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    File.WriteAllText(outPath, data);
                }
                i++;
                if (verbose && i % 10 == 0)
                    Console.WriteLine("[+] processed " + i + "/" + Files.Count + " = " + Math.Round((i/(double)Files.Count) * 100) + "%");
            }
        }
    }

}