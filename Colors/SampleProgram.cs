using System;
using System.Threading.Tasks;

namespace Colors
{
    /// <summary>
    /// Console sample running on .NET Core.
    /// Should be portable to .NET Framework without hassle.
    /// For demonstration purpose only!
    /// </summary>
    internal class SampleProgram
    {
        private static async Task Main()
        {
            var console = new ConsolePrinter();

            var updater = new MaterialDesignColors();
            await PreviewColorsInConsole(console, updater).ConfigureAwait(false);
        }

        private static async Task PreviewColorsInConsole(IPalettePrinter printer, IPalettesProvider provider)
        {
            var palettes = await provider.DownloadOnlinePalettes().ConfigureAwait(false);

            Console.WriteLine($"Got {palettes.Count} palettes.");
            Console.WriteLine();

            foreach (var palette in palettes)
            {
                printer.PrintPalette(palette, true);

                Console.WriteLine();
                Console.ReadKey();
                Console.WriteLine();
            }
        }
    }
}
