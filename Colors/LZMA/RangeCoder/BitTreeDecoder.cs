namespace SevenZip.Compression.RangeCoder
{
    internal struct BitTreeDecoder
    {
        private readonly BitDecoder[] models;
        private readonly int numBitLevels;

        public BitTreeDecoder(int levels)
        {
            numBitLevels = levels;
            models = new BitDecoder[1 << levels];
        }

        public void Init()
        {
            for (uint i = 1; i < (1 << numBitLevels); i++)
                models[i].Init();
        }

        public uint Decode(RangeCoder.Decoder rangeDecoder)
        {
            uint m = 1;
            for (var bitIndex = numBitLevels; bitIndex > 0; bitIndex--)
                m = (m << 1) + models[m].Decode(rangeDecoder);
            return m - ((uint) 1 << numBitLevels);
        }

        public uint ReverseDecode(RangeCoder.Decoder rangeDecoder)
        {
            uint m = 1;
            uint symbol = 0;
            for (var bitIndex = 0; bitIndex < numBitLevels; bitIndex++)
            {
                var bit = models[m].Decode(rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= bit << bitIndex;
            }
            return symbol;
        }

        public static uint ReverseDecode(BitDecoder[] Models, uint startIndex,
            RangeCoder.Decoder rangeDecoder, int NumBitLevels)
        {
            uint m = 1;
            uint symbol = 0;
            for (var bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
            {
                var bit = Models[startIndex + m].Decode(rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= bit << bitIndex;
            }
            return symbol;
        }
    }
}
