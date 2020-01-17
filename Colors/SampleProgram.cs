using System;
using System.IO;
using System.Threading.Tasks;

using Colors.Visualization;

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
            var output = File.OpenWrite($"{DateTime.Now.ToString("yyyyMMdd HHmmss")}.yaml");
            IPalettesProvider reader = new JapaneseTraditionalColors();
            await PrintColorsToFile(new LocalStorageWriter(output), reader);

            var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "Cache", "Palettes.yaml");

            var file = File.Open(inputPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            file.Seek(0, SeekOrigin.Begin);

            reader = new LocalStorageReader(file);
            await PreviewColorsInConsole(reader);

            Console.WriteLine("Done.");
        }

        private static async Task PreviewColorsInConsole(IPalettesProvider provider)
        {
            var printer = new ConsolePrinter();
            var palettes = await provider.RetrievePalettesAsync().ConfigureAwait(false);

            Console.WriteLine($"Got {palettes.Count} palettes.");
            Console.WriteLine();

            foreach (var palette in palettes)
            {
                await printer.PrintPaletteAsync(palette, true).ConfigureAwait(false);

                Console.WriteLine();
                Console.ReadKey();
                Console.WriteLine();
            }
        }

        private static async Task PrintColorsToFile(LocalStorageWriter printer, IPalettesProvider provider)
        {
            var palettes = await provider.RetrievePalettesAsync().ConfigureAwait(false);

            await printer.PrintPalettesAsync(palettes).ConfigureAwait(false);
            await printer.DisposeAsync().ConfigureAwait(false);

            Console.WriteLine($"Wrote {palettes.Count} palettes.");
            Console.WriteLine();
        }
    }
}
