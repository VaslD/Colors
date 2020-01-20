using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Colors.Core;

namespace Colors.Visualization
{
    /// <summary>
    /// A <see cref="IPalettePrinter"/> that generates a <see href="http://colorpeek.com">ColorPeek</see> permalink
    /// from given palette.
    /// </summary>
    public class ColorPeekWebPrinter : IPalettePrinter
    {
        public const string TargetURL = "http://colorpeek.com/#{0}";

        public IDisposable Target
            => throw new NotSupportedException("This printer has no specific target. It opens system-default web browser.");

        public ValueTask PrintPaletteAsync(Palette palette, bool flushWhenDone = false)
        {
            var colorParams = string.Join(',', palette.Colors);
            Process.Start(new ProcessStartInfo {
                FileName = string.Format(TargetURL, colorParams),
                UseShellExecute = true
            });
            return default;
        }

        public ValueTask DisposeAsync() => default;
    }
}
