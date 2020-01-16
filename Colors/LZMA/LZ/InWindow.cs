namespace SevenZip.Compression.LZ
{
    public class InWindow
    {
        public byte[] bufferBase = null;
        private System.IO.Stream stream;
        private uint posLimit;
        private bool streamEndWasReached;

        private uint pointerToLastSafePosition;

        public uint bufferOffset;

        public uint blockSize;
        public uint pos;
        private uint keepSizeBefore;
        private uint keepSizeAfter;
        public uint streamPos;

        public void MoveBlock()
        {
            var offset = bufferOffset + pos - keepSizeBefore;
            if (offset > 0)
                offset--;

            var numBytes = bufferOffset + streamPos - offset;

            for (uint i = 0; i < numBytes; i++)
                bufferBase[i] = bufferBase[offset + i];
            bufferOffset -= offset;
        }

        public virtual void ReadBlock()
        {
            if (streamEndWasReached)
                return;
            while (true)
            {
                var size = (int) (0 - bufferOffset + blockSize - streamPos);
                if (size == 0)
                    return;
                var numReadBytes = stream.Read(bufferBase, (int) (bufferOffset + streamPos), size);
                if (numReadBytes == 0)
                {
                    posLimit = streamPos;
                    var pointerToPostion = bufferOffset + posLimit;
                    if (pointerToPostion > pointerToLastSafePosition)
                        posLimit = pointerToLastSafePosition - bufferOffset;

                    streamEndWasReached = true;
                    return;
                }
                streamPos += (uint) numReadBytes;
                if (streamPos >= pos + keepSizeAfter)
                    posLimit = streamPos - keepSizeAfter;
            }
        }

        private void Free()
        {
            bufferBase = null;
        }

        public void Create(uint keepSizeBefore, uint keepSizeAfter, uint keepSizeReserv)
        {
            this.keepSizeBefore = keepSizeBefore;
            this.keepSizeAfter = keepSizeAfter;
            var blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
            if (bufferBase == null || this.blockSize != blockSize)
            {
                Free();
                this.blockSize = blockSize;
                bufferBase = new byte[this.blockSize];
            }
            pointerToLastSafePosition = this.blockSize - keepSizeAfter;
        }

        public void SetStream(System.IO.Stream stream)
        {
            this.stream = stream;
        }

        public void ReleaseStream()
        {
            stream = null;
        }

        public void Init()
        {
            bufferOffset = 0;
            pos = 0;
            streamPos = 0;
            streamEndWasReached = false;
            ReadBlock();
        }

        public void MovePos()
        {
            pos++;
            if (pos > posLimit)
            {
                var pointerToPostion = bufferOffset + pos;
                if (pointerToPostion > pointerToLastSafePosition)
                    MoveBlock();
                ReadBlock();
            }
        }

        public byte GetIndexByte(int index)
        {
            return bufferBase[bufferOffset + pos + index];
        }

        public uint GetMatchLen(int index, uint distance, uint limit)
        {
            if (streamEndWasReached)
                if (pos + index + limit > streamPos)
                    limit = streamPos - (uint) (pos + index);
            distance++;
            var pby = bufferOffset + pos + (uint) index;

            uint i;
            for (i = 0; i < limit && bufferBase[pby + i] == bufferBase[pby + i - distance]; i++) ;
            return i;
        }

        public uint GetNumAvailableBytes()
        {
            return streamPos - pos;
        }

        public void ReduceOffsets(int subValue)
        {
            bufferOffset += (uint) subValue;
            posLimit -= (uint) subValue;
            pos -= (uint) subValue;
            streamPos -= (uint) subValue;
        }
    }
}
