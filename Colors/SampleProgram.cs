using System;
using System.Threading.Tasks;

using Pastel;

namespace Colors
{
    internal class SampleProgram
    {
        /// <summary>
        /// .NET Core console sample.
        /// </summary>
        /// <returns></returns>
        private static async Task Main()
        {
            IPalettesProvider provider = new WarframeColors();
            await PreviewColors(provider).ConfigureAwait(false);

            provider = new MaterialDesignColors();
            await PreviewColors(provider).ConfigureAwait(false);
        }

        private static async Task PreviewColors(IPalettesProvider provider)
        {
            var palettes = await provider.DownloadOnlinePalettes().ConfigureAwait(false);

            Console.WriteLine($"Got {palettes.Count} palettes.");
            Console.WriteLine();

            foreach (var palette in palettes)
            {
                Console.Write(palette.Name);
                var colors = palette.Colors;

                var index = 0;
                foreach (var color in colors)
                {
                    if (index % 10 == 0) Console.Write(Environment.NewLine + "  ");
                    if (string.IsNullOrEmpty(color.Name)) Console.Write("█".Pastel(color.Value));
                    else Console.Write(color.Name.Pastel(color.Value) + ", ");
                    index += 1;
                }

                Console.WriteLine();
                Console.ReadKey();
                Console.WriteLine();
            }
        }
    }
}
