using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace everadix
{

    public class CodeZip : IEnumerable<CodeFile>
    {
        private readonly List<CodeFile> _files; 
        
        public CodeZip(string prefix, Stream source)
        {
            var zip = new ZipFile(source);
            _files = new List<CodeFile>((int)zip.Count);
            foreach (ZipEntry entry in zip)
            {
                if (!entry.Name.EndsWith(".pyj"))
                    continue;
                var stream = zip.GetInputStream(entry);
                var data = stream.ReadAllBytes();
                var decrypted = Crypto.Decrypt(data);

                // really CCP, zlib deflate compressed data in a freaking zip?
                if (decrypted[0] == 0x78)
                    decrypted = Utility.Decompress(decrypted);

                // cut off the 'j' from '.pyj'
                var name = entry.Name.Substring(0, entry.Name.Length - 1);
                _files.Add(new CodeFile(CodeType.Library, prefix + "\\" + name.Replace('/', '\\'), decrypted));
            }
        }

        public IEnumerator<CodeFile> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}