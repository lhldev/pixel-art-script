using System.Diagnostics;
using System.Numerics;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;

using Rectangle = SixLabors.ImageSharp.Rectangle;
using Path = System.IO.Path;

namespace StarvingArtistsScript
{
    public static class ImageHelper
    {
        public static int RoundValue = 16;
        private const int ProcessDimention = 2048;

        public static void DisplayImage(this Image<Rgb24> image)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "preview.png");
            image.Save(tempPath);

            string winPath = tempPath.Replace("/", "\\");

            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{winPath}\"",
                UseShellExecute = true
            });

        }

        public static Image<Rgb24> Preprocess(this Image<Rgb24> image)
        {
            if (image.Width == image.Height)
            {
                // Already 1:1
                return image;
            }

            int minDimension = Math.Min(image.Width, image.Height);

            int x = (image.Width - minDimension) / 2;
            int y = (image.Height - minDimension) / 2;

            return image.Clone(ctx =>
            {
                ctx.Crop(new Rectangle(x, y, minDimension, minDimension));
                ctx.Resize(new ResizeOptions { Size = new Size(ProcessDimention, ProcessDimention), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.Lanczos3 });
            });
        }

        public static Image<Rgb24> Preview(this Image<Rgb24> image)
        {
            Debug.Assert(image.Width == image.Height);
            return image.Clone(ctx => ctx.Resize(new ResizeOptions { Size = new Size(ProcessDimention, ProcessDimention), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.NearestNeighbor }));
        }

        public static Image<Rgb24> Pixelate(this Image<Rgb24> image)
        {
            Debug.Assert(image.Width == image.Height);
            return image.Clone(ctx => ctx.Resize(new ResizeOptions { Size = new Size(32, 32), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.Box }));
        }

        public static float PixelSize(this Image<Rgb24> image)
        {
            Debug.Assert(image.Width == image.Height);
            return image.Width / 32.0f;
        }

        public static double Rmse(this Image<Rgb24> image, Image<Rgb24> original)
        {
            Debug.Assert(image.Width == original.Width);
            Debug.Assert(image.Height == original.Height);
            double sum = 0.0f;
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    double valueR = (double)image[i, j].R - original[i, j].R;
                    double valueG = (double)image[i, j].G - original[i, j].G;
                    double valueB = (double)image[i, j].B - original[i, j].B;
                    sum += valueR * valueR;
                    sum += valueG * valueG;
                    sum += valueB * valueB;
                }
            }
            return Math.Sqrt(sum / (image.Width * image.Height * 3));
        }

        public static IPath GenShape(this Image<Rgb24> image, Vector2 pos, int size)
        {
            float pixelSize = image.PixelSize();
            int centerX = (int)MathF.Round((pos.X + 0.5f) * pixelSize);
            int centerY = (int)MathF.Round((pos.Y + 0.5f) * pixelSize);
            float radius = pixelSize / 2.0f * size;

            return new EllipsePolygon(centerX, centerY, radius);
        }

        // Returns the rmse of the resulting shape
        public static double ProcessShape(this Image<Rgb24> original, IPath shape, Image<L8> excludeMask, out Rgb24 color)
        {
            using var mask = new Image<L8>(original.Width, original.Height);
            mask.Mutate(ctx => ctx.Fill(Color.White, shape));

            RectangleF bounds = shape.Bounds;
            int xMin = Math.Max(0, (int)Math.Floor(bounds.Left));
            int yMin = Math.Max(0, (int)Math.Floor(bounds.Top));
            int xMax = Math.Min(original.Width - 1, (int)Math.Ceiling(bounds.Right));
            int yMax = Math.Min(original.Height - 1, (int)Math.Ceiling(bounds.Bottom));

            long rSum = 0, gSum = 0, bSum = 0;
            int count = 0;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    if (mask[x, y].PackedValue >= 128 && excludeMask[x, y].PackedValue < 128)
                    {
                        Rgb24 pixel = original[x, y];
                        rSum += pixel.R;
                        gSum += pixel.G;
                        bSum += pixel.B;
                        count++;
                    }
                }
            }

            Debug.Assert(count != 0);

            double rAvg = (double)rSum / count;
            double gAvg = (double)gSum / count;
            double bAvg = (double)bSum / count;

            // Rmse
            double sum = 0.0f;
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    if (mask[x, y].PackedValue >= 128 && excludeMask[x, y].PackedValue < 128)
                    {
                        Rgb24 pixel = original[x, y];
                        double valueR = rAvg - pixel.R;
                        double valueG = gAvg - pixel.G;
                        double valueB = bAvg - pixel.B;
                        sum += valueR * valueR;
                        sum += valueG * valueG;
                        sum += valueB * valueB;
                    }
                }
            }
            color = new Rgb24((byte)Math.Round(rAvg), (byte)Math.Round(gAvg), (byte)Math.Round(bAvg));
            return Math.Sqrt(sum / (count * 3));
        }

        public static Image<Rgb24> GenImage(this Image<Rgb24> preview, Image<Rgb24> original)
        {
            Image<Rgb24> processing = preview.Clone();
            var excludeMask = new Image<L8>(processing.Width, processing.Height);
            Rgb24 colorOut;
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    IPath shape = processing.GenShape(new Vector2(x, y), 1);
                    original.ProcessShape(shape, excludeMask, out colorOut);
                    processing.Mutate(ctx => ctx.Fill(colorOut, shape));
                }
            }

            return processing;
        }
    }
}
