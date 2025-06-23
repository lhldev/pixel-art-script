using System.Drawing;
using System.Numerics;

namespace StarvingArtistScript
{
    public class PixelToDraw
    {
        public Color color { get; set; }
        public Vector2 point { get; set; }
        public PixelToDraw(Color colord, Vector2 pointd)
        {
            color = colord;
            point = pointd;
        }
    }
}
