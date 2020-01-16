using System;

namespace SevenZip
{
    internal class DataErrorException : ApplicationException
    {
        public DataErrorException() : base("Data error.")
        {
        }
    }

    internal class InvalidParamException : ApplicationException
    {
        public InvalidParamException() : base("Invalid parameter.")
        {
        }
    }

    public interface ICodeProgress
    {
        void SetProgress(long inSize, long outSize);
    };

    public interface ICoder
    {
        void Code(System.IO.Stream inStream, System.IO.Stream outStream, long inSize, long outSize, ICodeProgress progress);
    };

    public enum CoderPropID
    {
        /// <summary>
        /// Specifies default property.
        /// </summary>
        DefaultProp = 0,

        /// <summary>
        /// Specifies size of dictionary.
        /// </summary>
        DictionarySize,

        /// <summary>
        /// Specifies size of memory for PPM*.
        /// </summary>
        UsedMemorySize,

        /// <summary>
        /// Specifies order for PPM methods.
        /// </summary>
        Order,

        /// <summary>
        /// Specifies Block Size.
        /// </summary>
        BlockSize,

        /// <summary>
        /// Specifies number of postion state bits for LZMA (0 <= x <= 4).
        /// </summary>
        PosStateBits,

        /// <summary>
        /// Specifies number of literal context bits for LZMA (0 <= x <= 8).
        /// </summary>
        LitContextBits,

        /// <summary>
        /// Specifies number of literal position bits for LZMA (0 <= x <= 4).
        /// </summary>
        LitPosBits,

        /// <summary>
        /// Specifies number of fast bytes for LZ*.
        /// </summary>
        NumFastBytes,

        /// <summary>
        /// Specifies match finder. LZMA: "BT2", "BT4" or "BT4B".
        /// </summary>
        MatchFinder,

        /// <summary>
        /// Specifies the number of match finder cyckes.
        /// </summary>
        MatchFinderCycles,

        /// <summary>
        /// Specifies number of passes.
        /// </summary>
        NumPasses,

        /// <summary>
        /// Specifies number of algorithm.
        /// </summary>
        Algorithm,

        /// <summary>
        /// Specifies the number of threads.
        /// </summary>
        NumThreads,

        /// <summary>
        /// Specifies mode with end marker.
        /// </summary>
        EndMarker
    };

    public interface ISetCoderProperties
    {
        void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
    };

    public interface IWriteCoderProperties
    {
        void WriteCoderProperties(System.IO.Stream outStream);
    }

    public interface ISetDecoderProperties
    {
        void SetDecoderProperties(byte[] properties);
    }
}
