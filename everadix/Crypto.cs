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
                                                 0x1f, 0x2a, 0x6b, 0x83, 0x6d, 0x9b, 0xec, 0x3d, 0x97, 0x23, 0x73, 0x94,
                                                 0xc8, 0xcd, 0x38, 0x3e, 0x4f, 0x01, 0x3d, 0xc1, 0xc2, 0xf7, 0x62, 0x26,
                                                 0x00, 0x65, 0x2b, 0xfb, 0x98, 0x8f, 0x42, 0x9a, 0xc9, 0x52, 0x09, 0xa9,
                                                 0x6e, 0x6c, 0xfe, 0xe8, 0xea, 0x6b, 0x86, 0x74, 0x35, 0x1d, 0x8c, 0x05,
                                                 0x55, 0xb3, 0x28, 0x5a, 0xa2, 0x15, 0xda, 0xf6, 0x43, 0x5d, 0x8b, 0xeb,
                                                 0x05, 0x42, 0xfc, 0x80, 0x44, 0x91, 0xb8, 0x3e, 0x34, 0x3c, 0x13, 0x73,
                                                 0xf1, 0xc0, 0x5d, 0x24, 0x35, 0xbb, 0x44, 0xf3, 0x5e, 0x59, 0xf0, 0x08,
                                                 0xb9, 0x57, 0x25, 0xd9, 0xcb, 0xfd, 0xc7, 0x29, 0x44, 0xde, 0xa9, 0xe9,
                                                 0xa4, 0xcf, 0x9f, 0xd4, 0x3d, 0x09, 0xb7, 0xb0, 0x86, 0x2f, 0x44, 0xa1,
                                                 0x1a, 0x09, 0x59, 0x18, 0x01, 0x63, 0x54, 0xb1, 0x86, 0x84, 0xab, 0x8e,
                                                 0xdd, 0x9c, 0x38, 0xfd, 0x58, 0xfd, 0x02, 0x00
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