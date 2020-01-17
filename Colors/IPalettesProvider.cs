using System.Collections.Generic;
using System.Threading.Tasks;

using Colors.Core;

namespace Colors
{
    /// <summary>
    /// Basic interface of a color palette updater.
    /// </summary>
    public interface IPalettesProvider
    {
        ValueTask<IReadOnlyList<Palette>> RetrievePalettesAsync();
    }
}
