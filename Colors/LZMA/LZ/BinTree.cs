using System;

namespace SevenZip.Compression.LZ
{
    public class BinTree : InWindow, IMatchFinder
    {
        private uint cyclicBufferPos;
        private uint cyclicBufferSize = 0;
        private uint matchMaxLen;

        private uint[] son;
        private uint[] hash;

        private uint cutValue = 0xFF;
        private uint hashMask;
        private uint hashSizeSum = 0;

        private bool hashArray = true;

        private const uint kHash2Size = 1 << 10;
        private const uint kHash3Size = 1 << 16;
        private const uint kBT2HashSize = 1 << 16;
        private const uint kStartMaxLen = 1;
        private const uint kHash3Offset = kHash2Size;
        private const uint kEmptyHashValue = 0;
        private const uint kMaxValForNormalize = ((uint) 1 << 31) - 1;

        private uint kNumHashDirectBytes = 0;
        private uint kMinMatchCheck = 4;
        private uint kFixHashSize = kHash2Size + kHash3Size;

        public void SetType(int numHashBytes)
        {
            hashArray = numHashBytes > 2;
            if (hashArray)
            {
                kNumHashDirectBytes = 0;
                kMinMatchCheck = 4;
                kFixHashSize = kHash2Size + kHash3Size;
            }
            else
            {
                kNumHashDirectBytes = 2;
                kMinMatchCheck = 2 + 1;
                kFixHashSize = 0;
            }
        }

        public new void SetStream(System.IO.Stream stream)
        {
            base.SetStream(stream);
        }

        public new void ReleaseStream()
        {
            base.ReleaseStream();
        }

        public new void Init()
        {
            base.Init();
            for (uint i = 0; i < hashSizeSum; i++)
                hash[i] = kEmptyHashValue;
            cyclicBufferPos = 0;
            ReduceOffsets(-1);
        }

        public new void MovePos()
        {
            if (++cyclicBufferPos >= cyclicBufferSize)
                cyclicBufferPos = 0;
            base.MovePos();
            if (pos == kMaxValForNormalize)
                Normalize();
        }

        public new byte GetIndexByte(int index)
        {
            return base.GetIndexByte(index);
        }

        public new uint GetMatchLen(int index, uint distance, uint limit)
        { return base.GetMatchLen(index, distance, limit); }

        public new uint GetNumAvailableBytes()
        {
            return base.GetNumAvailableBytes();
        }

        public void Create(uint historySize, uint keepAddBufferBefore,
                uint matchMaxLen, uint keepAddBufferAfter)
        {
            if (historySize > kMaxValForNormalize - 256)
                throw new Exception();
            cutValue = 16 + (matchMaxLen >> 1);

            var windowReservSize = (historySize + keepAddBufferBefore +
                    matchMaxLen + keepAddBufferAfter) / 2 + 256;

            base.Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, windowReservSize);

            this.matchMaxLen = matchMaxLen;

            var cyclicBufferSize = historySize + 1;
            if (this.cyclicBufferSize != cyclicBufferSize)
                son = new uint[(this.cyclicBufferSize = cyclicBufferSize) * 2];

            var hs = kBT2HashSize;

            if (hashArray)
            {
                hs = historySize - 1;
                hs |= hs >> 1;
                hs |= hs >> 2;
                hs |= hs >> 4;
                hs |= hs >> 8;
                hs >>= 1;
                hs |= 0xFFFF;
                if (hs > (1 << 24))
                    hs >>= 1;
                hashMask = hs;
                hs++;
                hs += kFixHashSize;
            }
            if (hs != hashSizeSum)
                hash = new uint[hashSizeSum = hs];
        }

        public uint GetMatches(uint[] distances)
        {
            uint lenLimit;
            if (pos + matchMaxLen <= streamPos)
                lenLimit = matchMaxLen;
            else
            {
                lenLimit = streamPos - pos;
                if (lenLimit < kMinMatchCheck)
                {
                    MovePos();
                    return 0;
                }
            }

            uint offset = 0;
            var matchMinPos = (pos > cyclicBufferSize) ? (pos - cyclicBufferSize) : 0;
            var cur = bufferOffset + pos;
            var maxLen = kStartMaxLen; // to avoid items for len < hashSize;
            uint hashValue, hash2Value = 0, hash3Value = 0;

            if (hashArray)
            {
                var temp = CRC.table[bufferBase[cur]] ^ bufferBase[cur + 1];
                hash2Value = temp & (kHash2Size - 1);
                temp ^= (uint) bufferBase[cur + 2] << 8;
                hash3Value = temp & (kHash3Size - 1);
                hashValue = (temp ^ (CRC.table[bufferBase[cur + 3]] << 5)) & hashMask;
            }
            else
                hashValue = bufferBase[cur] ^ ((uint) bufferBase[cur + 1] << 8);

            var curMatch = hash[kFixHashSize + hashValue];
            if (hashArray)
            {
                var curMatch2 = hash[hash2Value];
                var curMatch3 = hash[kHash3Offset + hash3Value];
                hash[hash2Value] = pos;
                hash[kHash3Offset + hash3Value] = pos;
                if (curMatch2 > matchMinPos)
                    if (bufferBase[bufferOffset + curMatch2] == bufferBase[cur])
                    {
                        distances[offset++] = maxLen = 2;
                        distances[offset++] = pos - curMatch2 - 1;
                    }
                if (curMatch3 > matchMinPos)
                    if (bufferBase[bufferOffset + curMatch3] == bufferBase[cur])
                    {
                        if (curMatch3 == curMatch2)
                            offset -= 2;
                        distances[offset++] = maxLen = 3;
                        distances[offset++] = pos - curMatch3 - 1;
                        curMatch2 = curMatch3;
                    }
                if (offset != 0 && curMatch2 == curMatch)
                {
                    offset -= 2;
                    maxLen = kStartMaxLen;
                }
            }

            hash[kFixHashSize + hashValue] = pos;

            var ptr0 = (cyclicBufferPos << 1) + 1;
            var ptr1 = cyclicBufferPos << 1;

            uint len0, len1;
            len0 = len1 = kNumHashDirectBytes;

            if (kNumHashDirectBytes != 0)
            {
                if (curMatch > matchMinPos)
                {
                    if (bufferBase[bufferOffset + curMatch + kNumHashDirectBytes] !=
                            bufferBase[cur + kNumHashDirectBytes])
                    {
                        distances[offset++] = maxLen = kNumHashDirectBytes;
                        distances[offset++] = pos - curMatch - 1;
                    }
                }
            }

            var count = cutValue;

            while (true)
            {
                if (curMatch <= matchMinPos || count-- == 0)
                {
                    son[ptr0] = son[ptr1] = kEmptyHashValue;
                    break;
                }
                var delta = pos - curMatch;
                var cyclicPos = ((delta <= cyclicBufferPos) ?
                            (cyclicBufferPos - delta) :
                            (cyclicBufferPos - delta + cyclicBufferSize)) << 1;

                var pby1 = bufferOffset + curMatch;
                var len = Math.Min(len0, len1);
                if (bufferBase[pby1 + len] == bufferBase[cur + len])
                {
                    while (++len != lenLimit)
                        if (bufferBase[pby1 + len] != bufferBase[cur + len])
                            break;
                    if (maxLen < len)
                    {
                        distances[offset++] = maxLen = len;
                        distances[offset++] = delta - 1;
                        if (len == lenLimit)
                        {
                            son[ptr1] = son[cyclicPos];
                            son[ptr0] = son[cyclicPos + 1];
                            break;
                        }
                    }
                }
                if (bufferBase[pby1 + len] < bufferBase[cur + len])
                {
                    son[ptr1] = curMatch;
                    ptr1 = cyclicPos + 1;
                    curMatch = son[ptr1];
                    len1 = len;
                }
                else
                {
                    son[ptr0] = curMatch;
                    ptr0 = cyclicPos;
                    curMatch = son[ptr0];
                    len0 = len;
                }
            }
            MovePos();
            return offset;
        }

        public void Skip(uint num)
        {
            do
            {
                uint lenLimit;
                if (pos + matchMaxLen <= streamPos)
                    lenLimit = matchMaxLen;
                else
                {
                    lenLimit = streamPos - pos;
                    if (lenLimit < kMinMatchCheck)
                    {
                        MovePos();
                        continue;
                    }
                }

                var matchMinPos = (pos > cyclicBufferSize) ? (pos - cyclicBufferSize) : 0;
                var cur = bufferOffset + pos;

                uint hashValue;

                if (hashArray)
                {
                    var temp = CRC.table[bufferBase[cur]] ^ bufferBase[cur + 1];
                    var hash2Value = temp & (kHash2Size - 1);
                    hash[hash2Value] = pos;
                    temp ^= (uint) bufferBase[cur + 2] << 8;
                    var hash3Value = temp & (kHash3Size - 1);
                    hash[kHash3Offset + hash3Value] = pos;
                    hashValue = (temp ^ (CRC.table[bufferBase[cur + 3]] << 5)) & hashMask;
                }
                else
                    hashValue = bufferBase[cur] ^ ((uint) bufferBase[cur + 1] << 8);

                var curMatch = hash[kFixHashSize + hashValue];
                hash[kFixHashSize + hashValue] = pos;

                var ptr0 = (cyclicBufferPos << 1) + 1;
                var ptr1 = cyclicBufferPos << 1;

                uint len0, len1;
                len0 = len1 = kNumHashDirectBytes;

                var count = cutValue;
                while (true)
                {
                    if (curMatch <= matchMinPos || count-- == 0)
                    {
                        son[ptr0] = son[ptr1] = kEmptyHashValue;
                        break;
                    }

                    var delta = pos - curMatch;
                    var cyclicPos = ((delta <= cyclicBufferPos) ?
                                (cyclicBufferPos - delta) :
                                (cyclicBufferPos - delta + cyclicBufferSize)) << 1;

                    var pby1 = bufferOffset + curMatch;
                    var len = Math.Min(len0, len1);
                    if (bufferBase[pby1 + len] == bufferBase[cur + len])
                    {
                        while (++len != lenLimit)
                            if (bufferBase[pby1 + len] != bufferBase[cur + len])
                                break;
                        if (len == lenLimit)
                        {
                            son[ptr1] = son[cyclicPos];
                            son[ptr0] = son[cyclicPos + 1];
                            break;
                        }
                    }
                    if (bufferBase[pby1 + len] < bufferBase[cur + len])
                    {
                        son[ptr1] = curMatch;
                        ptr1 = cyclicPos + 1;
                        curMatch = son[ptr1];
                        len1 = len;
                    }
                    else
                    {
                        son[ptr0] = curMatch;
                        ptr0 = cyclicPos;
                        curMatch = son[ptr0];
                        len0 = len;
                    }
                }
                MovePos();
            }
            while (--num != 0);
        }

        private void NormalizeLinks(uint[] items, uint numItems, uint subValue)
        {
            for (uint i = 0; i < numItems; i++)
            {
                var value = items[i];
                if (value <= subValue)
                    value = kEmptyHashValue;
                else
                    value -= subValue;
                items[i] = value;
            }
        }

        private void Normalize()
        {
            var subValue = pos - cyclicBufferSize;
            NormalizeLinks(son, cyclicBufferSize * 2, subValue);
            NormalizeLinks(hash, hashSizeSum, subValue);
            ReduceOffsets((int) subValue);
        }

        public void SetCutValue(uint cutValue)
        {
            this.cutValue = cutValue;
        }
    }
}
