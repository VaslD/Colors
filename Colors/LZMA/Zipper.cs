using System;
using System.IO;

namespace SevenZip.Compression.LZMA
{
    public static class Zipper
    {
        public static byte[] Decompress(Stream inStream)
        {
            var decoder = new Decoder();

            var properties = new byte[5];
            if (inStream.Read(properties, 0, 5) != 5) throw new ApplicationException("LZMA input is too short.");
            long outSize = 0;
            for (var i = 0; i < 8; i++)
            {
                var v = inStream.ReadByte();
                if (v < 0) throw new ApplicationException("Can't read from stream.");
                outSize |= ((long) (byte) v) << (8 * i);
            }
            decoder.SetDecoderProperties(properties);

            var newOutStream = new MemoryStream((int) outSize);
            var compressedSize = inStream.Length - inStream.Position;
            decoder.Code(inStream, newOutStream, compressedSize, outSize, null);

            return newOutStream.ToArray();
        }
    }
}
