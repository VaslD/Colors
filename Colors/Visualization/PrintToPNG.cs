using System;
using System.IO;
using System.Threading.Tasks;

using Colors.Core;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;

namespace Colors.Visualization
{
    public class PrintToPNG : IPalettePrinter, IDisposable, IAsyncDisposable
    {
        public FileStream Target { get; }

        IDisposable IPalettePrinter.Target { get; }

        public PrintToPNG(string filePath) => Target = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        public PrintToPNG(FileStream file) => Target = file;

        private Image<Rgba32> GenerateImage(Palette palette, int groupByCount, int colorSampleWidthHeight = 100,
            int dividerWidth = 0, bool preferLandscape = true)
        {
            var sectors = (int) Math.Round(palette.Count / (double) groupByCount, MidpointRounding.ToPositiveInfinity);

            Image<Rgba32> image;
            if (dividerWidth == 0)
            {
                image = new Image<Rgba32>(5 * colorSampleWidthHeight, sectors * colorSampleWidthHeight);
            }
            else
            {
                image = new Image<Rgba32>(5 * (colorSampleWidthHeight + dividerWidth) - dividerWidth,
                    sectors * (colorSampleWidthHeight + dividerWidth) - dividerWidth);
                image.Mutate(context => context.Fill(SixLabors.ImageSharp.Color.Black));
            }

            var x = 0;
            var y = 0;
            for (var i = 0; i < palette.Count; i++)
            {
                var color = palette[i];

                if (i != 0 && i % 5 == 0)
                {
                    x = 0;
                    y += 100 + dividerWidth;
                }

                image.Mutate(context => context.Fill((SixLabors.ImageSharp.Color) color, new RectangularPolygon(x, y, 100, 100)));

                x += 100 + dividerWidth;
            }

            if (preferLandscape && image.Width < image.Height) image.Mutate(context => context.Rotate(RotateMode.Rotate270));

            return image;
        }

        public ValueTask PrintPaletteAsync(Palette palette, bool flushWhenDone = false)
        {
            return new ValueTask(Task.Run(() => {
                var image = GenerateImage(palette, 5);

                image.Save(Target, new PngEncoder {
                    CompressionLevel = 9,
                    ColorType = PngColorType.RgbWithAlpha
                });

                if (flushWhenDone) Target.Flush();
            }));
        }

        public ValueTask PrintPaletteAsync(Palette palette, int dividerWidth, bool flushWhenDone = false)
        {
            return new ValueTask(Task.Run(() => {
                var image = GenerateImage(palette, 5, 100, dividerWidth);

                image.Save(Target, new PngEncoder {
                    CompressionLevel = 9,
                    ColorType = PngColorType.RgbWithAlpha
                });

                if (flushWhenDone) Target.Flush();
            }));
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) Target.Dispose();
        }

        public void Dispose() => Dispose(true);

        #endregion IDisposable

        #region IAsyncDisposable

        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            if (disposing) return Target.DisposeAsync();
            return default;
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsync(true);
        }

        #endregion IAsyncDisposable
    }
}
