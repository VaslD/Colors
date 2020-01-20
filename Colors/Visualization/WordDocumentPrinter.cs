using System;
using System.Threading.Tasks;

using Colors.Core;

using DocumentFormat.OpenXml.Packaging;

namespace Colors.Visualization
{
    /// <summary>
    /// Printer for generating real-world printable Microsoft Office Word (DOCX) documents, using OpenXML library.
    /// </summary>
    public sealed class WordDocumentPrinter : IPalettePrinter, IDisposable
    {
        public WordprocessingDocument Target { get; }

        IDisposable IPalettePrinter.Target => Target;

        public ValueTask PrintPaletteAsync(Palette palette, bool flushWhenDone = false)
        {
            if (flushWhenDone && OpenXmlPackage.CanSave) Target.Save();

            return default;
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (disposing) Target.Dispose();
        }

        public void Dispose() => Dispose(true);

        #endregion IDisposable
    }
}
