using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Colors.Core;
using Colors.Visualization;

using YamlDotNet.RepresentationModel;

namespace Colors
{
    /// <summary>
    /// A printer that serializes in-memory palettes to a local file.
    /// These palettes can be brought back to in-memory objects via <see cref="LocalStorageReader"/>.
    /// </summary>
    public class LocalStorageWriter : IPalettePrinter
    {
        public StreamWriter Target { get; private set; }

        IDisposable IPalettePrinter.Target { get; }

        public LocalStorageWriter(FileStream file)
        {
            Target = new StreamWriter(file, Encoding.UTF8);
        }

        /// <summary>
        /// <para>Serialize a single <see cref="Palette"/> to a YAML document according to the following format:</para>
        /// <para>
        /// Named <see cref="Palette"/> is serialized as a single key-value pair (<see cref="YamlMappingNode"/>),
        /// where the key is <see cref="Palette.Name"/>,
        /// and the value is <see cref="Palette.Colors"/> list as <see cref="YamlSequenceNode"/>.
        /// </para>
        /// <para>
        /// Anonymous <see cref="Palette"/> is serialized as a list (<see cref="YamlSequenceNode"/>),
        /// where each element is a color in <see cref="Palette.Colors"/>.
        /// </para>
        /// <para>
        /// Named <see cref="Color"/> in <see cref="Palette.Colors"/> is serialized as
        /// a single key-value pair (<see cref="YamlMappingNode"/>),
        /// where the key is <see cref="Color.Name"/>,
        /// and the value is <see cref="Color.Value"/> as 4-byte integer (A/R/G/B).
        /// </para>
        /// <para>
        /// Anonymous <see cref="Color"/> is serialized as a 4-byte integer (A/R/G/B).
        /// </para>
        /// </summary>
        private YamlDocument Serialize(Palette palette)
        {
            var root = new YamlMappingNode();
            var sequence = new YamlSequenceNode();

            foreach (var color in palette.Colors)
            {
                if (string.IsNullOrEmpty(color.Name)) sequence.Add(new YamlScalarNode {
                    Value = color.Value.ToArgb().ToString(CultureInfo.InvariantCulture)
                });
                else sequence.Add(new YamlMappingNode(color.Name, color.Value.ToArgb().ToString(CultureInfo.InvariantCulture)));
            }

            root.Add(palette.Name, sequence);
            return new YamlDocument(root);
        }

        public async ValueTask PrintPaletteAsync(Palette palette, bool flushWhenDone = false)
        {
            var document = Serialize(palette);
            var streamable = new YamlStream(document);
            streamable.Save(Target, false);
            await Target.WriteLineAsync().ConfigureAwait(false);
        }

        public async Task PrintPalettesAsync(IEnumerable<Palette> palettes)
        {
            foreach (var palette in palettes) await PrintPaletteAsync(palette).ConfigureAwait(false);
        }

        #region IAsyncDisposable

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (disposing) await Target.DisposeAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true).ConfigureAwait(false);
        }

        #endregion IAsyncDisposable
    }
}
