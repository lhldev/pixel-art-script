using System.Diagnostics;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace StarvingArtistsScript
{
    public static class ImageHelper
    {
        public static int RoundValue = 16;

        // TEMP
        public static void DisplayImage(Image<Rgb24> image) {
            string tempPath = Path.Combine(Path.GetTempPath(), "preview.png");
            image.Save(tempPath);

            // Convert to Windows path format (C:\...)
            string winPath = tempPath.Replace("/", "\\");  // extra step if needed

            System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                    FileName = "explorer.exe",
                    Arguments = $"\"{winPath}\"",
                    UseShellExecute = true
                    });

        }

        public static Image<Rgb24> Crop1to1(Image<Rgb24> image)
        {
            if (image.Width == image.Height)
            {
                // Already 1:1
                return image;
            }

            int minDimension = Math.Min(image.Width, image.Height);

            int x = (image.Width - minDimension) / 2;
            int y = (image.Height - minDimension) / 2;

            return image.Clone(ctx => ctx.Crop(new Rectangle(x, y, minDimension, minDimension)));
        }

        public static Image<Rgb24> ResizeBack(Image<Rgb24> image, Image<Rgb24> original)
        {
            return image.Clone(ctx => ctx.Resize(new ResizeOptions{ Size = new Size(original.Width, original.Height), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.NearestNeighbor }));
        }

        public static Image<Rgb24> Resize(Image<Rgb24> image)
        {
            return image.Clone(ctx => ctx.Resize(new ResizeOptions{ Size = new Size(32, 32), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.Bicubic}));
        }
    }
}
