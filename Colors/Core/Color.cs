using System;

namespace Colors.Core
{
    /// <summary>
    /// A color with its dedicated name. Interoperable with <see cref="System.Drawing.Color"/>.
    /// </summary>
    public class Color : IEquatable<Color>, IEquatable<System.Drawing.Color>
    {
        public string Name { get; private set; }
        public System.Drawing.Color Value { get; private set; }

        public Color(string hex)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            Value = System.Drawing.Color.FromArgb(int.Parse(hex, System.Globalization.NumberStyles.HexNumber));

            if (Value.A == 0) Value = System.Drawing.Color.FromArgb(255, Value);
        }

        public Color(string name, string hex) : this(hex)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public Color(int color)
        {
            Value = System.Drawing.Color.FromArgb(color);

            if (Value.A == 0) Value = System.Drawing.Color.FromArgb(255, Value);
        }

        public Color(string name, int color) : this(color)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return Value.ToArgb().ToString("x6");
        }

        #region Interop

        public static explicit operator System.Drawing.Color(Color color) => color.Value;
        public static explicit operator int(Color color) => color.Value.ToArgb();

        public static explicit operator SixLabors.ImageSharp.Color(Color color) =>
            SixLabors.ImageSharp.Color.FromRgba(color.Value.R, color.Value.G, color.Value.B, color.Value.A);

        public override int GetHashCode() => Value.ToArgb();
        public override bool Equals(object obj) => Equals(obj as Color);
        public bool Equals(Color other) => other != null && Value.Equals(other.Value);
        public bool Equals(System.Drawing.Color other) => other != null && Value.Equals(other);

        #endregion Interop
    }
}
