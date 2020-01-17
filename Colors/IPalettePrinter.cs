using System;

using Colors.Core;

namespace Colors
{
    /// <summary>
    /// Basic interface of a stream writer that serializes/visualizes color palettes. 
    /// </summary>
    public interface IPalettePrinter : IDisposable
    {
        /// <summary>
        /// The target of this printer.
        /// Usually a <see cref="System.IO.Stream"/>, <see cref="System.IO.TextWriter"/> or third-party document.
        /// </summary>
        IDisposable Target { get; }

        /// <summary>
        /// Prints the given color <paramref name="palette"/> to this printer's <see cref="Target"/>.
        /// Optionally, flush (save) this change immediately; otherwise let the target handle the underlying write.
        /// </summary>
        void PrintPalette(Palette palette, bool flushWhenDone = false);
    }
}
