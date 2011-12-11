using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace everadix
{

    /// <summary>
    /// handles code packages, i.e. compiled.code
    /// </summary>
    public class CodePackage : IEnumerable<CodeFile>
    {
        private readonly List<CodeFile> _files; 
        public DateTime Timestamp { get; private set; }
   
        public CodePackage(Stream source)
        {
            // hash, package, crc32?
            var wrapping = Unpickler.Load(source) as PyTuple;

            var data = wrapping.Items[1] as PyString;
            var core = Unpickler.Load(data.Data) as PyDict;
            Timestamp = (core["timestamp"] as PyLong).DateTime;
            var code = core["code"] as PyList;

            _files = new List<CodeFile>(code.Items.Count);
            foreach (var item in code.Items)
            {
                var jumbledCode = (((item as PyTuple).Items[0] as PyTuple).Items[1] as PyString).Data;
                var path = (((item as PyTuple).Items[1] as PyTuple).Items[1] as PyString).Value;
                var decrypted = Crypto.Decrypt(jumbledCode);
                // compressed
                if (decrypted[0] == 0x78)
                    decrypted = Utility.Decompress(decrypted);

                // sanitize the path "(root|script):../"
                var delimiter = path.IndexOf(':');
                if (delimiter > 0 && delimiter < 10)
                    path = path.Substring(delimiter + 1);
                if (path.StartsWith(@"/../"))
                    path = path.Substring(3);
                path = path.Replace('/', '\\');
                _files.Add(new CodeFile(CodeType.Game, path, decrypted, missingHeader: true));
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