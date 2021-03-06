namespace SevenZip.Compression.RangeCoder
{
    internal struct BitTreeEncoder
    {
        private readonly BitEncoder[] models;
        private readonly int numBitLevels;

        public BitTreeEncoder(int levels)
        {
            numBitLevels = levels;
            models = new BitEncoder[1 << levels];
        }

        public void Init()
        {
            for (uint i = 1; i < (1 << numBitLevels); i++)
                models[i].Init();
        }

        public void Encode(Encoder rangeEncoder, uint symbol)
        {
            uint m = 1;
            for (var bitIndex = numBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                var bit = (symbol >> bitIndex) & 1;
                models[m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
            }
        }

        public void ReverseEncode(Encoder rangeEncoder, uint symbol)
        {
            uint m = 1;
            for (uint i = 0; i < numBitLevels; i++)
            {
                var bit = symbol & 1;
                models[m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }

        public uint GetPrice(uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (var bitIndex = numBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                var bit = (symbol >> bitIndex) & 1;
                price += models[m].GetPrice(bit);
                m = (m << 1) + bit;
            }
            return price;
        }

        public uint ReverseGetPrice(uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (var i = numBitLevels; i > 0; i--)
            {
                var bit = symbol & 1;
                symbol >>= 1;
                price += models[m].GetPrice(bit);
                m = (m << 1) | bit;
            }
            return price;
        }

        public static uint ReverseGetPrice(BitEncoder[] Models, uint startIndex,
            int NumBitLevels, uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (var i = NumBitLevels; i > 0; i--)
            {
                var bit = symbol & 1;
                symbol >>= 1;
                price += Models[startIndex + m].GetPrice(bit);
                m = (m << 1) | bit;
            }
            return price;
        }

        public static void ReverseEncode(BitEncoder[] Models, uint startIndex,
            Encoder rangeEncoder, int NumBitLevels, uint symbol)
        {
            uint m = 1;
            for (var i = 0; i < NumBitLevels; i++)
            {
                var bit = symbol & 1;
                Models[startIndex + m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }
    }
}
