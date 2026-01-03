using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace StarvingArtistsScript
{
    public static class ShapeOptimizer
    {
        private const int GridSize = 32;
        private const int MaxShapeSize = 3;

        /// <summary>
        /// Optimizes shape placement using reverse-order greedy strategy
        /// </summary>
        public static List<ShapeInfo> OptimizeShapes(Image<Rgb24> original, Image<Rgb24> pixelated)
        {
            Debug.Assert(original.Width == original.Height);
            Debug.Assert(pixelated.Width == GridSize && pixelated.Height == GridSize);

            List<ShapeInfo> shapes = new List<ShapeInfo>();
            float pixelSize = original.PixelSize();

            // Phase 1: Background Precomputation
            Rgb24[,] backgroundColors = ComputeBaselineColors(original, pixelSize);

            // Phase 2: Reverse Shape Selection
            using var occlusionMask = new Image<L8>(original.Width, original.Height);
            
            // Iterate in reverse rendering order (1023 down to 0)
            for (int cellIndex = GridSize * GridSize - 1; cellIndex >= 0; cellIndex--)
            {
                int x = cellIndex % GridSize;
                int y = cellIndex / GridSize;
                Vector2 cellPos = new Vector2(x, y);

                ShapeInfo? bestShape = EvaluateCell(
                    original, 
                    cellPos, 
                    pixelSize, 
                    backgroundColors[x, y], 
                    occlusionMask
                );

                if (bestShape != null)
                {
                    shapes.Add(bestShape);
                    
                    // Update occlusion mask
                    IPath shapePath = GenerateShapePath(original, bestShape.Position, bestShape.Size);
                    occlusionMask.Mutate(ctx => ctx.Fill(Color.White, shapePath));
                }
                else
                {
                    // No shape, just background
                    shapes.Add(new ShapeInfo(ShapeType.None, cellPos, 0, new Rgb24(0, 0, 0), backgroundColors[x, y]));
                }
            }

            // Phase 3: Global Background Finalization
            FinalizeBackgrounds(shapes, original, pixelSize, occlusionMask);

            // Reverse the list to get proper rendering order (0 to 1023)
            shapes.Reverse();

            return shapes;
        }

        private static Rgb24[,] ComputeBaselineColors(Image<Rgb24> original, float pixelSize)
        {
            Rgb24[,] colors = new Rgb24[GridSize, GridSize];

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    int xMin = (int)Math.Floor(x * pixelSize);
                    int yMin = (int)Math.Floor(y * pixelSize);
                    int xMax = (int)Math.Ceiling((x + 1) * pixelSize) - 1;
                    int yMax = (int)Math.Ceiling((y + 1) * pixelSize) - 1;

                    xMin = Math.Max(0, xMin);
                    yMin = Math.Max(0, yMin);
                    xMax = Math.Min(original.Width - 1, xMax);
                    yMax = Math.Min(original.Height - 1, yMax);

                    long rSum = 0, gSum = 0, bSum = 0;
                    int count = 0;

                    for (int py = yMin; py <= yMax; py++)
                    {
                        for (int px = xMin; px <= xMax; px++)
                        {
                            Rgb24 pixel = original[px, py];
                            rSum += pixel.R;
                            gSum += pixel.G;
                            bSum += pixel.B;
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        colors[x, y] = new Rgb24(
                            (byte)(rSum / count),
                            (byte)(gSum / count),
                            (byte)(bSum / count)
                        );
                    }
                }
            }

            return colors;
        }

        private static ShapeInfo? EvaluateCell(
            Image<Rgb24> original,
            Vector2 cellPos,
            float pixelSize,
            Rgb24 backgroundColor,
            Image<L8> occlusionMask)
        {
            // Calculate ROI (Region of Interest) - bounding box for max shape size
            float maxRadius = pixelSize / 2.0f * MaxShapeSize;
            int centerX = (int)Math.Round((cellPos.X + 0.5f) * pixelSize);
            int centerY = (int)Math.Round((cellPos.Y + 0.5f) * pixelSize);

            int roiXMin = Math.Max(0, (int)Math.Floor(centerX - maxRadius));
            int roiYMin = Math.Max(0, (int)Math.Floor(centerY - maxRadius));
            int roiXMax = Math.Min(original.Width - 1, (int)Math.Ceiling(centerX + maxRadius));
            int roiYMax = Math.Min(original.Height - 1, (int)Math.Ceiling(centerY + maxRadius));

            // Get visible pixels (not occluded)
            List<(int x, int y)> visiblePixels = new List<(int, int)>();
            for (int py = roiYMin; py <= roiYMax; py++)
            {
                for (int px = roiXMin; px <= roiXMax; px++)
                {
                    if (occlusionMask[px, py].PackedValue < 128)
                    {
                        visiblePixels.Add((px, py));
                    }
                }
            }

            if (visiblePixels.Count == 0)
                return null;

            // Compute baseline error (no shape)
            double baselineError = 0;
            foreach (var (px, py) in visiblePixels)
            {
                Rgb24 pixel = original[px, py];
                double dr = pixel.R - backgroundColor.R;
                double dg = pixel.G - backgroundColor.G;
                double db = pixel.B - backgroundColor.B;
                baselineError += dr * dr + dg * dg + db * db;
            }

            // Evaluate all shape candidates
            ShapeInfo? bestShape = null;
            double bestImprovement = 0;

            for (int size = 1; size <= MaxShapeSize; size++)
            {
                IPath shapePath = GenerateShapePath(original, cellPos, size);
                
                // Create shape mask
                using var shapeMask = new Image<L8>(original.Width, original.Height);
                shapeMask.Mutate(ctx => ctx.Fill(Color.White, shapePath));

                // Identify foreground pixels
                List<(int x, int y)> fgPixels = new List<(int, int)>();
                foreach (var (px, py) in visiblePixels)
                {
                    if (shapeMask[px, py].PackedValue >= 128)
                    {
                        fgPixels.Add((px, py));
                    }
                }

                if (fgPixels.Count == 0)
                    continue;

                // Compute foreground color (mean of foreground pixels)
                long rSum = 0, gSum = 0, bSum = 0;
                foreach (var (px, py) in fgPixels)
                {
                    Rgb24 pixel = original[px, py];
                    rSum += pixel.R;
                    gSum += pixel.G;
                    bSum += pixel.B;
                }

                Rgb24 fgColor = new Rgb24(
                    (byte)(rSum / fgPixels.Count),
                    (byte)(gSum / fgPixels.Count),
                    (byte)(bSum / fgPixels.Count)
                );

                // Compute error with shape
                double shapeError = 0;
                foreach (var (px, py) in visiblePixels)
                {
                    Rgb24 pixel = original[px, py];
                    Rgb24 targetColor = shapeMask[px, py].PackedValue >= 128 ? fgColor : backgroundColor;
                    
                    double dr = pixel.R - targetColor.R;
                    double dg = pixel.G - targetColor.G;
                    double db = pixel.B - targetColor.B;
                    shapeError += dr * dr + dg * dg + db * db;
                }

                double improvement = baselineError - shapeError;
                if (improvement > bestImprovement)
                {
                    bestImprovement = improvement;
                    bestShape = new ShapeInfo(ShapeType.Circle, cellPos, size, fgColor, backgroundColor);
                }
            }

            return bestShape;
        }

        private static void FinalizeBackgrounds(
            List<ShapeInfo> shapes,
            Image<Rgb24> original,
            float pixelSize,
            Image<L8> occlusionMask)
        {
            // Forward pass to refine background colors
            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                ShapeInfo shape = shapes[i];
                int x = (int)shape.Position.X;
                int y = (int)shape.Position.Y;

                // Get cell boundaries
                int xMin = (int)Math.Floor(x * pixelSize);
                int yMin = (int)Math.Floor(y * pixelSize);
                int xMax = (int)Math.Ceiling((x + 1) * pixelSize) - 1;
                int yMax = (int)Math.Ceiling((y + 1) * pixelSize) - 1;

                xMin = Math.Max(0, xMin);
                yMin = Math.Max(0, yMin);
                xMax = Math.Min(original.Width - 1, xMax);
                yMax = Math.Min(original.Height - 1, yMax);

                // Find unlocked pixels in this cell
                long rSum = 0, gSum = 0, bSum = 0;
                int count = 0;

                for (int py = yMin; py <= yMax; py++)
                {
                    for (int px = xMin; px <= xMax; px++)
                    {
                        if (occlusionMask[px, py].PackedValue < 128)
                        {
                            Rgb24 pixel = original[px, py];
                            rSum += pixel.R;
                            gSum += pixel.G;
                            bSum += pixel.B;
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    shape.BackgroundColor = new Rgb24(
                        (byte)(rSum / count),
                        (byte)(gSum / count),
                        (byte)(bSum / count)
                    );
                }
            }
        }

        private static IPath GenerateShapePath(Image<Rgb24> image, Vector2 pos, int size)
        {
            float pixelSize = image.PixelSize();
            float centerX = (pos.X + 0.5f) * pixelSize;
            float centerY = (pos.Y + 0.5f) * pixelSize;
            float radius = pixelSize / 2.0f * size;

            return new EllipsePolygon(centerX, centerY, radius);
        }
    }
}
