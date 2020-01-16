using System.Collections.Generic;
using System.Threading.Tasks;

using Colors.Core;

namespace Colors
{
    /// <summary>
    /// Base interface of a color palette updater. For use in generic collections.
    /// </summary>
    public interface IPalettesProvider
    {
        Task<IReadOnlyList<Palette>> DownloadOnlinePalettes();
    }
}
