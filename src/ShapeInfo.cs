using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace StarvingArtistsScript
{
    public enum ShapeType
    {
        None,
        Circle
        // Future: Triangle, Diamond
    }

    public class ShapeInfo
    {
        public ShapeType Type { get; set; }
        public Vector2 Position { get; set; } // Grid position (0-31)
        public int Size { get; set; } // 1, 2, or 3
        public Rgb24 ForegroundColor { get; set; }
        public Rgb24 BackgroundColor { get; set; }

        public ShapeInfo(ShapeType type, Vector2 position, int size, Rgb24 fgColor, Rgb24 bgColor)
        {
            Type = type;
            Position = position;
            Size = size;
            ForegroundColor = fgColor;
            BackgroundColor = bgColor;
        }
    }
}
