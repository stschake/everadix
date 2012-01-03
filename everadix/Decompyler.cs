using System.Diagnostics;
using System.IO;
using System.Text;
using everadix.Properties;

namespace everadix
{

    /// <summary>
    /// we don't decompyle ourselves, but instead delegate it to an external python script
    /// </summary>
    public static class Decompyler
    {
        /// <summary>
        /// Python 2.7 magic for use when processing files that don't have any magic 
        /// </summary>
        private static readonly byte[] Magic = new byte[] { 0x03, 0xF3, 0x0D, 0x0A, 0x14, 0x7E, 0x0D, 0x4B };

        private static readonly string PythonBase = Settings.Default.PythonBase;
        private static readonly string PythonExecutable = Settings.Default.PythonExecutable;
        private static readonly string DecompylerScript = Settings.Default.DecompylerScript;

        public static void VerifySetup()
        {
            if (!File.Exists(PythonBase + PythonExecutable))
                throw new FileNotFoundException("Couldn't find python executable");
            if (!File.Exists(PythonBase + "Scripts//" + DecompylerScript))
                throw new FileNotFoundException("Couldn't find decompyler python script");
        }

        public static string Decompyle(byte[] data, bool missingHeader = false)
        {
            // we need to write the data to a temporary file or the script sadly won't be able to process it
            using (var fs = File.Create(PythonBase + "temp.pyc"))
            {
                var bw = new BinaryWriter(fs);
                if (missingHeader)
                    bw.Write(Magic);
                bw.Write(data);

                bw.Flush();
                fs.Flush(true);
            }

            var process = new Process
                              {
                                  StartInfo = new ProcessStartInfo(PythonBase + PythonExecutable,
                                                                   "Scripts/" + DecompylerScript + " temp.pyc")
                                                  {
                                                      CreateNoWindow = true,
                                                      WorkingDirectory = PythonBase,
                                                      RedirectStandardOutput = true,
                                                      UseShellExecute = false
                                                  }
                              };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            if (output.Contains("1 failed"))
                return null;
            // filter the extra script output
            var lines = output.Split('\n');
            var ret = new StringBuilder();
            // the first line is always script output
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("+++ okay"))
                    break;
                ret.Append(lines[i].Replace("\r\r\n", ""));
            }
            // all done!
            process.Dispose();
            return ret.ToString();
        }
    }

}