using System;
using System.IO;
using System.Threading.Tasks;

using Colors.Core;

using Pastel;

namespace Colors.Visualization
{
    /// <summary>
    /// A <see cref="IPalettePrinter"/> that lets you preview color palettes in system terminal.
    /// </summary>
    public sealed class ConsolePrinter : IPalettePrinter
    {
        public TextWriter Target { get; private set; }

        IDisposable IPalettePrinter.Target => Target;

        public ConsolePrinter() => Target = Console.Out;

        public ConsolePrinter(TextWriter writer) => Target = writer;

        public async ValueTask PrintPaletteAsync(Palette palette, bool flushWhenDone = false)
        {
            await Target.WriteAsync($"Palette: {palette.Name}").ConfigureAwait(false);

            var colors = palette.Colors;
            for (var i = 0; i < colors.Count; i++)
            {
                if (i % 5 == 0) await Target.WriteAsync(Target.NewLine).ConfigureAwait(false);

                var color = colors[i];
                if (string.IsNullOrEmpty(color.Name)) await Target.WriteAsync("█".Pastel(color.Value)).ConfigureAwait(false);
                else await Target.WriteAsync(color.Name.Pastel(color.Value) + " | ").ConfigureAwait(false);
            }

            await Target.WriteLineAsync().ConfigureAwait(false);

            if (flushWhenDone) await Target.FlushAsync().ConfigureAwait(false);
        }
    }
}
