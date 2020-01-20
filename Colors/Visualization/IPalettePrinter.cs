using System;
using System.Threading.Tasks;

using Colors.Core;

namespace Colors.Visualization
{
    /// <summary>
    /// Basic interface of a document writer that serializes/visualizes color palettes.
    /// </summary>
    public interface IPalettePrinter
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
        ValueTask PrintPaletteAsync(Palette palette, bool flushWhenDone = false);
    }
}
