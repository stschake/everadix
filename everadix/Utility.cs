﻿using System;
using System.IO;
using System.IO.Compression;

namespace everadix
{

    public static class Utility
    {

        // source for this code is http://geekswithblogs.net/sdorman/archive/2009/01/10/reading-all-bytes-from-a-stream.aspx, no explicit license given, but fair use should cover
        public static byte[] ReadAllBytes(this Stream source)
        {
            var readBuffer = new byte[4096];

            int totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = source.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    int nextByte = source.ReadByte();
                    if (nextByte != -1)
                    {
                        var temp = new byte[readBuffer.Length * 2];
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            byte[] buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }
            return buffer;
        }

        public static byte[] Decompress(byte[] input)
        {
            // two bytes shaved off (zlib header)
            var sourceStream = new MemoryStream(input, 2, input.Length - 2);
            var stream = new DeflateStream(sourceStream, CompressionMode.Decompress);
            return stream.ReadAllBytes();
        }
    }

}