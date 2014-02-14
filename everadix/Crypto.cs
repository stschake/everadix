using System;
using System.ComponentModel;
using System.IO;

namespace everadix
{

    public static class Crypto
    {

        // TODO: write static analyzer to pry this from blue.dlls' cold hands
        private static readonly byte[] BlueKey = new byte[]
                                             {
                                                0x01, 0x02, 0x00, 0x00, 0x03, 0x66, 0x00, 0x00, 0x00, 0xA4, 0x00, 0x00, 0x85, 0x91, 0xB5, 0x5B,
                                                0x37, 0x15, 0x57, 0xAE, 0x6B, 0x80, 0x2F, 0x57, 0xD3, 0x83, 0x91, 0x52, 0x20, 0x89, 0xC4, 0x7A,
                                                0x08, 0xB0, 0x64, 0xBA, 0x00, 0x92, 0xC9, 0x18, 0x37, 0xFC, 0xA8, 0x93, 0xC2, 0x28, 0x4B, 0x93,
                                                0xFB, 0xE3, 0xF6, 0xA7, 0xB2, 0x85, 0xC4, 0x7F, 0xAE, 0xAA, 0xCF, 0xDD, 0xDA, 0xFB, 0x41, 0x41,
                                                0x57, 0x42, 0x9C, 0x56, 0x19, 0x0F, 0xEF, 0x82, 0xE8, 0x0D, 0x38, 0x8E, 0x58, 0x2C, 0xCE, 0xBD,
                                                0x92, 0x6F, 0x51, 0x5A, 0xE6, 0x5F, 0xC8, 0x6E, 0x87, 0x88, 0xDB, 0x85, 0x38, 0xCB, 0x28, 0x35,
                                                0x43, 0xFD, 0x33, 0x5D, 0x91, 0xE9, 0x8A, 0xBF, 0x6B, 0x36, 0x32, 0x22, 0x58, 0x9B, 0x30, 0x0D,
                                                0x2F, 0xCC, 0xC0, 0xAB, 0x7D, 0xB5, 0xC8, 0x61, 0x86, 0xC4, 0x01, 0x36, 0x35, 0xE0, 0x59, 0x49,
                                                0xE2, 0xFB, 0xAC, 0xCE, 0xA5, 0x91, 0x8F, 0x4B, 0xED, 0xF9, 0x02, 0x00
                                             };

        private static IntPtr _blueKeyHandle;

        public static void Initialize()
        {
            IntPtr provider = IntPtr.Zero;
            Native.CryptAcquireContext(ref provider, null, "Microsoft Enhanced Cryptographic Provider v1.0",
                                       Native.PROV_RSA_FULL, Native.CRYPT_VERIFYCONTEXT);
            if (provider == IntPtr.Zero)
                throw new InvalidDataException("Couldn't acquire crypto context");

            IntPtr unkKey = IntPtr.Zero;
            if (!Native.CryptGenKey(provider, 1, 1, ref unkKey))
                throw new Exception("Couldn't generate a new key", new Win32Exception());
            int reqSpace = 0;
            Native.CryptExportKey(unkKey, IntPtr.Zero, 7, 0, null, ref reqSpace);
            var unkKeyData = new byte[reqSpace];
            if (!Native.CryptExportKey(unkKey, IntPtr.Zero, 7, 0, unkKeyData, ref reqSpace))
                throw new Exception("Couldn't export new key", new Win32Exception());

            // the hell is this supposed to do?
            var modulusBits = BitConverter.ToInt32(unkKeyData, 12);
            for (int i = 0; i < 4; i++)
            {
                if (i > 0)
                    unkKeyData[16 + i] = 0;
                else
                    unkKeyData[16] = 1;
            }
            var modulusBytes = modulusBits >> 3;
            var modulusWords = modulusBits >> 4;
            var offset = modulusBytes * 2 + 4;
            for (int i = 0; i < modulusWords; i++)
            {
                if (i > 0)
                    unkKeyData[offset] = 1;
                else
                    unkKeyData[offset + i] = 0;
            }
            offset += modulusWords;
            for (int i = 0; i < modulusWords; i++)
            {
                if (i > 0)
                    unkKeyData[offset] = 1;
                else
                    unkKeyData[offset + i] = 0;
            }
            offset += modulusBytes;
            for (int i = 0; i < modulusBytes; i++)
            {
                if (i > 0)
                    unkKeyData[offset] = 1;
                else
                    unkKeyData[offset + i] = 0;
            }

            IntPtr unkKeyBack = IntPtr.Zero;
            if (!Native.CryptImportKey(provider, unkKeyData, reqSpace, IntPtr.Zero, 0, ref unkKeyBack))
                throw new Exception("Couldn't import the new key back in", new Win32Exception());

            _blueKeyHandle = IntPtr.Zero;
            if (!Native.CryptImportKey(provider, BlueKey, 140, unkKeyBack, 0, ref _blueKeyHandle))
                throw new InvalidDataException("Failed to import blue key", new Win32Exception());
        }

        public static byte[] Decrypt(byte[] data)
        {
            var finalLength = (uint)data.Length;
            if (Native.CryptDecrypt(_blueKeyHandle, IntPtr.Zero, 1, 0, data, ref finalLength) <= 0)
                throw new InvalidDataException("Failed to decrypt data", new Win32Exception());
            Array.Resize(ref data, (int)finalLength);
            return data;
        }
    }

}