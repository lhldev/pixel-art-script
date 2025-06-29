using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace StarvingArtistsScript
{
    public class PixelToDraw
    {
        public Rgb24 color { get; set; }
        public Vector2 point { get; set; }
        public PixelToDraw(Rgb24 color, Vector2 point)
        {
            this.color = color;
            this.point = point;
        }
    }
}
