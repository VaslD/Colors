using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Colors.Serialization;
using Colors.Visualization;

namespace Colors
{
    /// <summary>
    /// Console sample running on .NET Core.
    /// Should be portable to .NET Framework without hassle.
    /// For demonstration purpose only.
    /// </summary>
    internal class SampleProgram
    {
        private static async Task Main()
        {
            var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "Cache", "Palettes.yaml");

            using var file = File.Open(inputPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            file.Seek(0, SeekOrigin.Begin);

            var reader = new LocalStorageReader(file);
            var palettes = await reader.RetrievePalettesAsync().ConfigureAwait(false);

            var imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Palettes");
            Directory.CreateDirectory(imagesPath);
            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            var palette = palettes[0];

            var imageFile = File.OpenWrite(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sample.png"));
            var printer = new PrintToPNG(imageFile);
            await printer.PrintPaletteAsync(palette, 2).ConfigureAwait(false);
            await printer.DisposeAsync().ConfigureAwait(false);
        }

        private static async Task PreviewColorsInConsole(IPalettesProvider provider)
        {
            var printer = new ConsolePrinter();
            var palettes = await provider.RetrievePalettesAsync().ConfigureAwait(false);

            Console.WriteLine($"Got {palettes.Count} palette(s).");
            Console.WriteLine();

            foreach (var palette in palettes)
            {
                await printer.PrintPaletteAsync(palette, true).ConfigureAwait(false);

                Console.WriteLine();
                Console.ReadKey();
                Console.WriteLine();
            }
        }

        private static async Task SendColorsToPrinter(IPalettePrinter printer, IPalettesProvider provider)
        {
            var palettes = await provider.RetrievePalettesAsync().ConfigureAwait(false);

            foreach (var palette in palettes) await printer.PrintPaletteAsync(palette, true).ConfigureAwait(false);
            await printer.DisposeAsync().ConfigureAwait(false);

            Console.WriteLine($"Wrote {palettes.Count} palette(s).");
            Console.WriteLine();
        }
    }
}
