using System;
using System.Collections;
using System.Collections.Generic;

namespace Colors.Core
{
    /// <summary>
    /// A read-only color palette with a collection of <see cref="Color"/>s and the name for this collection.
    /// </summary>
    public class Palette : IReadOnlyList<Color>
    {
        public string Name { get; private set; }
        public IReadOnlyList<Color> Colors { get; private set; }

        public Palette(string name, List<Color> colors)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Colors = colors.AsReadOnly() ?? throw new ArgumentNullException(nameof(colors));
        }

        #region Interface

        public Color this[int index] => Colors[index];
        public int Count => Colors.Count;
        public IEnumerator<Color> GetEnumerator() => Colors.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => (Colors as IEnumerable).GetEnumerator();

        #endregion Interface
    }
}
