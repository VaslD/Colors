using System;
using System.IO;

using Colors.Core;

using Pastel;

namespace Colors
{
    /// <summary>
    /// A printer that lets you preview color palettes in <see cref="Console"/>.
    /// </summary>
    public sealed class ConsolePrinter : IPalettePrinter
    {
        public TextWriter Target { get; private set; }

        IDisposable IPalettePrinter.Target => Target;

        public ConsolePrinter()
        {
            Target = Console.Out;
        }

        public ConsolePrinter(TextWriter writer)
        {
            Target = writer;
        }

        public void PrintPalette(Palette palette, bool flushWhenDone = false)
        {
            Target.Write($"Palette: {palette.Name}");

            var colors = palette.Colors;
            for (var i = 0; i < colors.Count; i++)
            {
                if (i % 5 == 0) Target.Write(Target.NewLine);

                var color = colors[i];
                if (string.IsNullOrEmpty(color.Name)) Target.Write("█".Pastel(color.Value));
                else Target.Write(color.Name.Pastel(color.Value) + " | ");
            }

            Target.WriteLine();

            if (flushWhenDone) Target.Flush();
        }

        public void Dispose()
        {
            // System.Console.Out should not be disposed.
            return;
        }
    }
}
