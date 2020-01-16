using System;
using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.LZMA
{
    public class Decoder : ICoder, ISetDecoderProperties
    {
        private class LenDecoder
        {
            private BitDecoder choice = new BitDecoder();
            private BitDecoder choice2 = new BitDecoder();
            private readonly BitTreeDecoder[] lowCoder = new BitTreeDecoder[Base.kNumPosStatesMax];
            private readonly BitTreeDecoder[] midCoder = new BitTreeDecoder[Base.kNumPosStatesMax];
            private readonly BitTreeDecoder highCoder = new BitTreeDecoder(Base.kNumHighLenBits);
            private uint numPosStates = 0;

            public void Create(uint states)
            {
                for (var posState = numPosStates; posState < states; posState++)
                {
                    lowCoder[posState] = new BitTreeDecoder(Base.kNumLowLenBits);
                    midCoder[posState] = new BitTreeDecoder(Base.kNumMidLenBits);
                }
                numPosStates = states;
            }

            public void Init()
            {
                choice.Init();
                for (uint posState = 0; posState < numPosStates; posState++)
                {
                    lowCoder[posState].Init();
                    midCoder[posState].Init();
                }
                choice2.Init();
                highCoder.Init();
            }

            public uint Decode(RangeCoder.Decoder rangeDecoder, uint posState)
            {
                if (choice.Decode(rangeDecoder) == 0)
                    return lowCoder[posState].Decode(rangeDecoder);
                else
                {
                    var symbol = Base.kNumLowLenSymbols;
                    if (choice2.Decode(rangeDecoder) == 0)
                        symbol += midCoder[posState].Decode(rangeDecoder);
                    else
                    {
                        symbol += Base.kNumMidLenSymbols;
                        symbol += highCoder.Decode(rangeDecoder);
                    }
                    return symbol;
                }
            }
        }

        private class LiteralDecoder
        {
            private struct Decoder2
            {
                private BitDecoder[] m_Decoders;

                public void Create()
                {
                    m_Decoders = new BitDecoder[0x300];
                }

                public void Init()
                {
                    for (var i = 0; i < 0x300; i++) m_Decoders[i].Init();
                }

                public byte DecodeNormal(RangeCoder.Decoder rangeDecoder)
                {
                    uint symbol = 1;
                    do
                        symbol = (symbol << 1) | m_Decoders[symbol].Decode(rangeDecoder);
                    while (symbol < 0x100);
                    return (byte) symbol;
                }

                public byte DecodeWithMatchByte(RangeCoder.Decoder rangeDecoder, byte matchByte)
                {
                    uint symbol = 1;
                    do
                    {
                        var matchBit = (uint) (matchByte >> 7) & 1;
                        matchByte <<= 1;
                        var bit = m_Decoders[((1 + matchBit) << 8) + symbol].Decode(rangeDecoder);
                        symbol = (symbol << 1) | bit;
                        if (matchBit != bit)
                        {
                            while (symbol < 0x100)
                                symbol = (symbol << 1) | m_Decoders[symbol].Decode(rangeDecoder);
                            break;
                        }
                    }
                    while (symbol < 0x100);
                    return (byte) symbol;
                }
            }

            private Decoder2[] m_Coders;
            private int m_NumPrevBits;
            private int m_NumPosBits;
            private uint m_PosMask;

            public void Create(int numPosBits, int numPrevBits)
            {
                if (m_Coders != null && m_NumPrevBits == numPrevBits &&
                    m_NumPosBits == numPosBits)
                    return;
                m_NumPosBits = numPosBits;
                m_PosMask = ((uint) 1 << numPosBits) - 1;
                m_NumPrevBits = numPrevBits;
                var numStates = (uint) 1 << (m_NumPrevBits + m_NumPosBits);
                m_Coders = new Decoder2[numStates];
                for (uint i = 0; i < numStates; i++)
                    m_Coders[i].Create();
            }

            public void Init()
            {
                var numStates = (uint) 1 << (m_NumPrevBits + m_NumPosBits);
                for (uint i = 0; i < numStates; i++)
                    m_Coders[i].Init();
            }

            private uint GetState(uint pos, byte prevByte)
            { return ((pos & m_PosMask) << m_NumPrevBits) + (uint) (prevByte >> (8 - m_NumPrevBits)); }

            public byte DecodeNormal(RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte)
            { return m_Coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder); }

            public byte DecodeWithMatchByte(RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
            { return m_Coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte); }
        };

        private readonly LZ.OutWindow outWindow = new LZ.OutWindow();
        private readonly RangeCoder.Decoder rangeDecoder = new RangeCoder.Decoder();

        private readonly BitDecoder[] isMatchDecoders = new BitDecoder[Base.kNumStates << Base.kNumPosStatesBitsMax];
        private readonly BitDecoder[] isRepDecoders = new BitDecoder[Base.kNumStates];
        private readonly BitDecoder[] isRepG0Decoders = new BitDecoder[Base.kNumStates];
        private readonly BitDecoder[] isRepG1Decoders = new BitDecoder[Base.kNumStates];
        private readonly BitDecoder[] isRepG2Decoders = new BitDecoder[Base.kNumStates];
        private readonly BitDecoder[] isRep0LongDecoders = new BitDecoder[Base.kNumStates << Base.kNumPosStatesBitsMax];

        private readonly BitTreeDecoder[] posSlotDecoder = new BitTreeDecoder[Base.kNumLenToPosStates];
        private readonly BitDecoder[] posDecoders = new BitDecoder[Base.kNumFullDistances - Base.kEndPosModelIndex];

        private readonly BitTreeDecoder posAlignDecoder = new BitTreeDecoder(Base.kNumAlignBits);

        private readonly LenDecoder lenDecoder = new LenDecoder();
        private readonly LenDecoder repLenDecoder = new LenDecoder();

        private readonly LiteralDecoder literalDecoder = new LiteralDecoder();

        private uint dictionarySize;
        private uint dictionarySizeCheck;

        private uint posStateMask;

        public Decoder()
        {
            dictionarySize = 0xFFFFFFFF;
            for (var i = 0; i < Base.kNumLenToPosStates; i++)
                posSlotDecoder[i] = new BitTreeDecoder(Base.kNumPosSlotBits);
        }

        private void SetDictionarySize(uint dictionarySize)
        {
            if (this.dictionarySize != dictionarySize)
            {
                this.dictionarySize = dictionarySize;
                dictionarySizeCheck = Math.Max(this.dictionarySize, 1);
                var blockSize = Math.Max(dictionarySizeCheck, 1 << 12);
                outWindow.Create(blockSize);
            }
        }

        private void SetLiteralProperties(int lp, int lc)
        {
            if (lp > 8)
                throw new InvalidParamException();
            if (lc > 8)
                throw new InvalidParamException();
            literalDecoder.Create(lp, lc);
        }

        private void SetPosBitsProperties(int pb)
        {
            if (pb > Base.kNumPosStatesBitsMax)
                throw new InvalidParamException();
            var numPosStates = (uint) 1 << pb;
            lenDecoder.Create(numPosStates);
            repLenDecoder.Create(numPosStates);
            posStateMask = numPosStates - 1;
        }

        private bool _solid = false;

        private void Init(System.IO.Stream inStream, System.IO.Stream outStream)
        {
            rangeDecoder.Init(inStream);
            outWindow.Init(outStream, _solid);

            uint i;
            for (i = 0; i < Base.kNumStates; i++)
            {
                for (uint j = 0; j <= posStateMask; j++)
                {
                    var index = (i << Base.kNumPosStatesBitsMax) + j;
                    isMatchDecoders[index].Init();
                    isRep0LongDecoders[index].Init();
                }
                isRepDecoders[i].Init();
                isRepG0Decoders[i].Init();
                isRepG1Decoders[i].Init();
                isRepG2Decoders[i].Init();
            }

            literalDecoder.Init();
            for (i = 0; i < Base.kNumLenToPosStates; i++)
                posSlotDecoder[i].Init();
            // m_PosSpecDecoder.Init();
            for (i = 0; i < Base.kNumFullDistances - Base.kEndPosModelIndex; i++)
                posDecoders[i].Init();

            lenDecoder.Init();
            repLenDecoder.Init();
            posAlignDecoder.Init();
        }

        public void Code(System.IO.Stream inStream, System.IO.Stream outStream,
            long inSize, long outSize, ICodeProgress progress)
        {
            Init(inStream, outStream);

            var state = new Base.State();
            state.Init();
            uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;

            ulong nowPos64 = 0;
            var outSize64 = (ulong) outSize;
            if (nowPos64 < outSize64)
            {
                if (isMatchDecoders[state.index << Base.kNumPosStatesBitsMax].Decode(rangeDecoder) != 0)
                    throw new DataErrorException();
                state.UpdateChar();
                var b = literalDecoder.DecodeNormal(rangeDecoder, 0, 0);
                outWindow.PutByte(b);
                nowPos64++;
            }
            while (nowPos64 < outSize64)
            {
                var posState = (uint) nowPos64 & posStateMask;
                if (isMatchDecoders[(state.index << Base.kNumPosStatesBitsMax) + posState].Decode(rangeDecoder) == 0)
                {
                    byte b;
                    var prevByte = outWindow.GetByte(0);
                    if (!state.IsCharState())
                        b = literalDecoder.DecodeWithMatchByte(rangeDecoder,
                            (uint) nowPos64, prevByte, outWindow.GetByte(rep0));
                    else
                        b = literalDecoder.DecodeNormal(rangeDecoder, (uint) nowPos64, prevByte);
                    outWindow.PutByte(b);
                    state.UpdateChar();
                    nowPos64++;
                }
                else
                {
                    uint len;
                    if (isRepDecoders[state.index].Decode(rangeDecoder) == 1)
                    {
                        if (isRepG0Decoders[state.index].Decode(rangeDecoder) == 0)
                        {
                            if (isRep0LongDecoders[(state.index << Base.kNumPosStatesBitsMax) + posState].Decode(rangeDecoder) == 0)
                            {
                                state.UpdateShortRep();
                                outWindow.PutByte(outWindow.GetByte(rep0));
                                nowPos64++;
                                continue;
                            }
                        }
                        else
                        {
                            uint distance;
                            if (isRepG1Decoders[state.index].Decode(rangeDecoder) == 0)
                            {
                                distance = rep1;
                            }
                            else
                            {
                                if (isRepG2Decoders[state.index].Decode(rangeDecoder) == 0)
                                    distance = rep2;
                                else
                                {
                                    distance = rep3;
                                    rep3 = rep2;
                                }
                                rep2 = rep1;
                            }
                            rep1 = rep0;
                            rep0 = distance;
                        }
                        len = repLenDecoder.Decode(rangeDecoder, posState) + Base.kMatchMinLen;
                        state.UpdateRep();
                    }
                    else
                    {
                        rep3 = rep2;
                        rep2 = rep1;
                        rep1 = rep0;
                        len = Base.kMatchMinLen + lenDecoder.Decode(rangeDecoder, posState);
                        state.UpdateMatch();
                        var posSlot = posSlotDecoder[Base.GetLenToPosState(len)].Decode(rangeDecoder);
                        if (posSlot >= Base.kStartPosModelIndex)
                        {
                            var numDirectBits = (int) ((posSlot >> 1) - 1);
                            rep0 = (2 | (posSlot & 1)) << numDirectBits;
                            if (posSlot < Base.kEndPosModelIndex)
                                rep0 += BitTreeDecoder.ReverseDecode(posDecoders,
                                        rep0 - posSlot - 1, rangeDecoder, numDirectBits);
                            else
                            {
                                rep0 += rangeDecoder.DecodeDirectBits(
                                    numDirectBits - Base.kNumAlignBits) << Base.kNumAlignBits;
                                rep0 += posAlignDecoder.ReverseDecode(rangeDecoder);
                            }
                        }
                        else
                            rep0 = posSlot;
                    }
                    if (rep0 >= outWindow.TrainSize + nowPos64 || rep0 >= dictionarySizeCheck)
                    {
                        if (rep0 == 0xFFFFFFFF)
                            break;
                        throw new DataErrorException();
                    }
                    outWindow.CopyBlock(rep0, len);
                    nowPos64 += len;
                }

            }
            outWindow.Flush();
            outWindow.ReleaseStream();
            rangeDecoder.ReleaseStream();
        }

        public void SetDecoderProperties(byte[] properties)
        {
            if (properties.Length < 5)
                throw new InvalidParamException();
            var lc = properties[0] % 9;
            var remainder = properties[0] / 9;
            var lp = remainder % 5;
            var pb = remainder / 5;
            if (pb > Base.kNumPosStatesBitsMax)
                throw new InvalidParamException();
            uint dictionarySize = 0;
            for (var i = 0; i < 4; i++)
                dictionarySize += ((uint) properties[1 + i]) << (i * 8);
            SetDictionarySize(dictionarySize);
            SetLiteralProperties(lp, lc);
            SetPosBitsProperties(pb);
        }

        public bool Train(System.IO.Stream stream)
        {
            _solid = true;
            return outWindow.Train(stream);
        }
    }
}
