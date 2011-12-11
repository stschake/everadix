using System;
using System.Runtime.InteropServices;

namespace everadix
{
    public static class Native
    {
        public const Int32 PROV_RSA_FULL = 0x00000001;
        public const Int32 CRYPT_VERIFYCONTEXT = -268435456;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CryptAcquireContext(ref IntPtr hProv, string pszContainer, string pszProvider,Int32 dwProvType, Int32 dwFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptImportKey(IntPtr hProv, Byte[] pbData, Int32 dwDataLen, IntPtr hPubKey, Int32 dwFlags, ref IntPtr phKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptGenKey(IntPtr hProv, Int32 alg, Int32 dwFlags, ref IntPtr phKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptExportKey(IntPtr hKey, IntPtr hExpKey, Int32 dwBlobType, Int32 dwFlags, [In, Out] Byte[] pbData,
            ref Int32 pdwDataLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int CryptEncrypt(IntPtr hKey, IntPtr hHash, Int32 final, UInt32 dwFlags, byte[] pbData, 
            ref UInt32 pdwDataLen, UInt32 dwBufLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int CryptDecrypt(IntPtr hKey, IntPtr hHash, Int32 final, UInt32 dwFlags, byte[] pbData, 
            ref UInt32 pdwDataLen);
    }

}