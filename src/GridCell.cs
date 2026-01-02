using SixLabors.ImageSharp.PixelFormats;

namespace StarvingArtistsScript
{
    public class GridCell
    {
        public Rgb24 BackgroundColor { get; set; }
        public ShapeType Shape { get; set; }
        public int ShapeSize { get; set; }
        public Rgb24 ForegroundColor { get; set; }

        public GridCell()
        {
            BackgroundColor = new Rgb24(255, 255, 255);
            Shape = ShapeType.None;
            ShapeSize = 0;
            ForegroundColor = new Rgb24(0, 0, 0);
        }
    }
}
