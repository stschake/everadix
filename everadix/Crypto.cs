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
                                                 0x01, 0x02, 0x00, 0x00, 0x03, 0x66, 0x00, 0x00, 0x00, 0xa4, 0x00, 0x00,
                                                 0x07, 0x83, 0x08, 0x07, 0xcd, 0x10, 0x10, 0xf8, 0xf8, 0xe0, 0x5e, 0xb3,
                                                 0x91, 0x68, 0x5d, 0xe3, 0x43, 0x25, 0xf7, 0x4a, 0xa2, 0x52, 0x10, 0x57,
                                                 0x00, 0xd6, 0x8f, 0x94, 0x68, 0x08, 0xae, 0x04, 0x2c, 0xd8, 0xae, 0x8b,
                                                 0x07, 0xaf, 0x7c, 0x95, 0x78, 0x6b, 0x3c, 0x2b, 0x79, 0x87, 0x12, 0xda,
                                                 0x20, 0x4d, 0xd8, 0x10, 0x94, 0x71, 0x6c, 0xd6, 0xf7, 0x31, 0x12, 0x4b,
                                                 0x2b, 0x13, 0xd3, 0x8e, 0x67, 0x63, 0xbe, 0xa5, 0x62, 0x2d, 0x3f, 0x52,
                                                 0x8d, 0x7c, 0x5f, 0xe8, 0x58, 0xb6, 0xbd, 0xde, 0xdc, 0x8f, 0x58, 0xb8,
                                                 0xd4, 0xfa, 0xb2, 0xde, 0xfa, 0xce, 0x66, 0x9a, 0xa8, 0x39, 0x14, 0x9b,
                                                 0xf0, 0x3a, 0x8d, 0xca, 0x41, 0x90, 0x39, 0x68, 0x27, 0xc9, 0x94, 0xba,
                                                 0xe1, 0x40, 0xaa, 0x79, 0x0b, 0x76, 0x2f, 0xcb, 0x70, 0x7f, 0x8d, 0x0a,
                                                 0x37, 0xed, 0x43, 0x9e, 0x94, 0x83, 0x02, 0x00
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
            var offset = modulusBytes*2 + 4;
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