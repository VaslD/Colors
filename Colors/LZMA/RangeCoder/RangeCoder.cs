namespace SevenZip.Compression.RangeCoder
{
    internal class Encoder
    {
        public const uint kTopValue = 1 << 24;

        private System.IO.Stream Stream;

        public ulong low;
        public uint range;
        private uint cacheSize;
        private byte cache;

        private long StartPosition;

        public void SetStream(System.IO.Stream stream)
        {
            Stream = stream;
        }

        public void ReleaseStream()
        {
            Stream = null;
        }

        public void Init()
        {
            StartPosition = Stream.Position;

            low = 0;
            range = 0xFFFFFFFF;
            cacheSize = 1;
            cache = 0;
        }

        public void FlushData()
        {
            for (var i = 0; i < 5; i++)
                ShiftLow();
        }

        public void FlushStream()
        {
            Stream.Flush();
        }

        public void CloseStream()
        {
            Stream.Close();
        }

        public void Encode(uint start, uint size, uint total)
        {
            low += start * (range /= total);
            range *= size;
            while (range < kTopValue)
            {
                range <<= 8;
                ShiftLow();
            }
        }

        public void ShiftLow()
        {
            if ((uint) low < 0xFF000000 || (uint) (low >> 32) == 1)
            {
                var temp = cache;
                do
                {
                    Stream.WriteByte((byte) (temp + (low >> 32)));
                    temp = 0xFF;
                }
                while (--cacheSize != 0);
                cache = (byte) (((uint) low) >> 24);
            }
            cacheSize++;
            low = ((uint) low) << 8;
        }

        public void EncodeDirectBits(uint v, int numTotalBits)
        {
            for (var i = numTotalBits - 1; i >= 0; i--)
            {
                range >>= 1;
                if (((v >> i) & 1) == 1)
                    low += range;
                if (range < kTopValue)
                {
                    range <<= 8;
                    ShiftLow();
                }
            }
        }

        public void EncodeBit(uint size0, int numTotalBits, uint symbol)
        {
            var newBound = (range >> numTotalBits) * size0;
            if (symbol == 0)
                range = newBound;
            else
            {
                low += newBound;
                range -= newBound;
            }
            while (range < kTopValue)
            {
                range <<= 8;
                ShiftLow();
            }
        }

        public long GetProcessedSizeAdd()
        {
            return cacheSize +
                Stream.Position - StartPosition + 4;
        }
    }

    internal class Decoder
    {
        public const uint kTopValue = 1 << 24;
        public uint Range;
        public uint Code;

        public System.IO.Stream Stream;

        public void Init(System.IO.Stream stream)
        {
            Stream = stream;

            Code = 0;
            Range = 0xFFFFFFFF;
            for (var i = 0; i < 5; i++)
                Code = (Code << 8) | (byte) Stream.ReadByte();
        }

        public void ReleaseStream()
        {
            Stream = null;
        }

        public void CloseStream()
        {
            Stream.Close();
        }

        public void Normalize()
        {
            while (Range < kTopValue)
            {
                Code = (Code << 8) | (byte) Stream.ReadByte();
                Range <<= 8;
            }
        }

        public void Normalize2()
        {
            if (Range < kTopValue)
            {
                Code = (Code << 8) | (byte) Stream.ReadByte();
                Range <<= 8;
            }
        }

        public uint GetThreshold(uint total)
        {
            return Code / (Range /= total);
        }

        public void Decode(uint start, uint size, uint total)
        {
            Code -= start * Range;
            Range *= size;
            Normalize();
        }

        public uint DecodeDirectBits(int numTotalBits)
        {
            var range = Range;
            var code = Code;
            uint result = 0;
            for (var i = numTotalBits; i > 0; i--)
            {
                range >>= 1;
                var t = (code - range) >> 31;
                code -= range & (t - 1);
                result = (result << 1) | (1 - t);

                if (range < kTopValue)
                {
                    code = (code << 8) | (byte) Stream.ReadByte();
                    range <<= 8;
                }
            }
            Range = range;
            Code = code;
            return result;
        }

        public uint DecodeBit(uint size0, int numTotalBits)
        {
            var newBound = (Range >> numTotalBits) * size0;
            uint symbol;
            if (Code < newBound)
            {
                symbol = 0;
                Range = newBound;
            }
            else
            {
                symbol = 1;
                Code -= newBound;
                Range -= newBound;
            }
            Normalize();
            return symbol;
        }
    }
}
