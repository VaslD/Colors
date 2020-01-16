namespace SevenZip.Compression.RangeCoder
{
    internal struct BitEncoder
    {
        public const int kNumBitModelTotalBits = 11;
        public const uint kBitModelTotal = 1 << kNumBitModelTotalBits;
        private const int kNumMoveBits = 5;
        private const int kNumMoveReducingBits = 2;
        public const int kNumBitPriceShiftBits = 6;

        private uint prob;

        public void Init()
        {
            prob = kBitModelTotal >> 1;
        }

        public void UpdateModel(uint symbol)
        {
            if (symbol == 0)
                prob += (kBitModelTotal - prob) >> kNumMoveBits;
            else
                prob -= (prob) >> kNumMoveBits;
        }

        public void Encode(Encoder encoder, uint symbol)
        {
            var newBound = (encoder.range >> kNumBitModelTotalBits) * prob;
            if (symbol == 0)
            {
                encoder.range = newBound;
                prob += (kBitModelTotal - prob) >> kNumMoveBits;
            }
            else
            {
                encoder.low += newBound;
                encoder.range -= newBound;
                prob -= (prob) >> kNumMoveBits;
            }
            if (encoder.range < Encoder.kTopValue)
            {
                encoder.range <<= 8;
                encoder.ShiftLow();
            }
        }

        private static readonly uint[] ProbPrices = new uint[kBitModelTotal >> kNumMoveReducingBits];

        static BitEncoder()
        {
            const int kNumBits = kNumBitModelTotalBits - kNumMoveReducingBits;
            for (var i = kNumBits - 1; i >= 0; i--)
            {
                var start = (uint) 1 << (kNumBits - i - 1);
                var end = (uint) 1 << (kNumBits - i);
                for (var j = start; j < end; j++)
                    ProbPrices[j] = ((uint) i << kNumBitPriceShiftBits) +
                        (((end - j) << kNumBitPriceShiftBits) >> (kNumBits - i - 1));
            }
        }

        public uint GetPrice(uint symbol)
        {
            return ProbPrices[(((prob - symbol) ^ (-(int) symbol)) & (kBitModelTotal - 1)) >> kNumMoveReducingBits];
        }

        public uint GetPrice0()
        {
            return ProbPrices[prob >> kNumMoveReducingBits];
        }

        public uint GetPrice1()
        {
            return ProbPrices[(kBitModelTotal - prob) >> kNumMoveReducingBits];
        }
    }

    internal struct BitDecoder
    {
        public const int kNumBitModelTotalBits = 11;
        public const uint kBitModelTotal = 1 << kNumBitModelTotalBits;
        private const int kNumMoveBits = 5;

        private uint Prob;

        public void UpdateModel(int numMoveBits, uint symbol)
        {
            if (symbol == 0)
                Prob += (kBitModelTotal - Prob) >> numMoveBits;
            else
                Prob -= (Prob) >> numMoveBits;
        }

        public void Init()
        {
            Prob = kBitModelTotal >> 1;
        }

        public uint Decode(RangeCoder.Decoder rangeDecoder)
        {
            var newBound = (rangeDecoder.Range >> kNumBitModelTotalBits) * Prob;
            if (rangeDecoder.Code < newBound)
            {
                rangeDecoder.Range = newBound;
                Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
                if (rangeDecoder.Range < Decoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte) rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 0;
            }
            else
            {
                rangeDecoder.Range -= newBound;
                rangeDecoder.Code -= newBound;
                Prob -= (Prob) >> kNumMoveBits;
                if (rangeDecoder.Range < Decoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte) rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 1;
            }
        }
    }
}
