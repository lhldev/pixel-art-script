using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace StarvingArtistsScript
{
    public class ImageReconstructor
    {
        private const int GridSize = 32;
        private Image<Rgb24> sourceImage;
        private GridCell[,] grid;
        private int cellWidth;
        private int cellHeight;

        public ImageReconstructor(Image<Rgb24> image)
        {
            this.sourceImage = image;
            this.grid = new GridCell[GridSize, GridSize];
            this.cellWidth = image.Width / GridSize;
            this.cellHeight = image.Height / GridSize;

            // Initialize grid
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    grid[x, y] = new GridCell();
                }
            }
        }

        public GridCell[,] Reconstruct()
        {
            // Phase 1: Background Precomputation
            ComputeBackgrounds();

            // Phase 2: Reverse Shape Selection
            ReverseShapeSelection();

            // Phase 3: Global Background Finalization
            FinalizeBackgrounds();

            return grid;
        }

        // Phase 1: Compute mean RGB for each cell
        private void ComputeBackgrounds()
        {
            for (int cellY = 0; cellY < GridSize; cellY++)
            {
                for (int cellX = 0; cellX < GridSize; cellX++)
                {
                    grid[cellX, cellY].BackgroundColor = ComputeCellMean(cellX, cellY);
                }
            }
        }

        private Rgb24 ComputeCellMean(int cellX, int cellY)
        {
            long rSum = 0, gSum = 0, bSum = 0;
            int count = 0;

            int startX = cellX * cellWidth;
            int startY = cellY * cellHeight;
            int endX = Math.Min(startX + cellWidth, sourceImage.Width);
            int endY = Math.Min(startY + cellHeight, sourceImage.Height);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Rgb24 pixel = sourceImage[x, y];
                    rSum += pixel.R;
                    gSum += pixel.G;
                    bSum += pixel.B;
                    count++;
                }
            }

            if (count == 0) return new Rgb24(255, 255, 255);

            return new Rgb24(
                (byte)(rSum / count),
                (byte)(gSum / count),
                (byte)(bSum / count)
            );
        }

        // Phase 2: Optimize shapes in reverse order
        private void ReverseShapeSelection()
        {
            bool[,] occlusionMask = new bool[sourceImage.Width, sourceImage.Height];

            // Iterate in reverse rendering order (row-major, but backwards)
            for (int cellY = GridSize - 1; cellY >= 0; cellY--)
            {
                for (int cellX = GridSize - 1; cellX >= 0; cellX--)
                {
                    OptimizeCell(cellX, cellY, occlusionMask);
                }
            }
        }

        private void OptimizeCell(int cellX, int cellY, bool[,] occlusionMask)
        {
            Rgb24 bgColor = grid[cellX, cellY].BackgroundColor;

            // Calculate baseline error (no shape)
            double baselineError = CalculateErrorForCell(cellX, cellY, occlusionMask, ShapeType.None, 0, bgColor, bgColor);

            // Try different shape candidates
            ShapeType bestShape = ShapeType.None;
            int bestSize = 0;
            Rgb24 bestFgColor = new Rgb24(0, 0, 0);
            double bestError = baselineError;

            // Try circle shapes with different sizes
            for (int size = 1; size <= 3; size++)
            {
                var (fgColor, error) = EvaluateShapeCandidate(cellX, cellY, occlusionMask, ShapeType.Circle, size, bgColor);
                double improvement = baselineError - error;

                if (improvement > 0 && error < bestError)
                {
                    bestShape = ShapeType.Circle;
                    bestSize = size;
                    bestFgColor = fgColor;
                    bestError = error;
                }
            }

            // Update grid cell with best option
            grid[cellX, cellY].Shape = bestShape;
            grid[cellX, cellY].ShapeSize = bestSize;
            grid[cellX, cellY].ForegroundColor = bestFgColor;

            // Update occlusion mask if shape was placed
            if (bestShape != ShapeType.None)
            {
                UpdateOcclusionMask(cellX, cellY, occlusionMask, bestShape, bestSize);
            }
        }

        private (Rgb24 fgColor, double error) EvaluateShapeCandidate(
            int cellX, int cellY, bool[,] occlusionMask, 
            ShapeType shape, int size, Rgb24 bgColor)
        {
            // Get shape mask
            bool[,] shapeMask = GetShapeMask(shape, size);
            
            // Compute foreground mean color
            Rgb24 fgColor = ComputeForegroundMean(cellX, cellY, occlusionMask, shapeMask, size);

            // Calculate total error
            double error = CalculateErrorWithShape(cellX, cellY, occlusionMask, shapeMask, size, bgColor, fgColor);

            return (fgColor, error);
        }

        private Rgb24 ComputeForegroundMean(int cellX, int cellY, bool[,] occlusionMask, bool[,] shapeMask, int shapeSize)
        {
            long rSum = 0, gSum = 0, bSum = 0;
            int count = 0;

            int centerX = cellX * cellWidth + cellWidth / 2;
            int centerY = cellY * cellHeight + cellHeight / 2;

            int maskSize = shapeMask.GetLength(0);
            int offset = maskSize / 2;

            for (int dy = 0; dy < maskSize; dy++)
            {
                for (int dx = 0; dx < maskSize; dx++)
                {
                    if (shapeMask[dx, dy])
                    {
                        int imgX = centerX - offset + dx;
                        int imgY = centerY - offset + dy;

                        if (imgX >= 0 && imgX < sourceImage.Width && imgY >= 0 && imgY < sourceImage.Height)
                        {
                            if (!occlusionMask[imgX, imgY])
                            {
                                Rgb24 pixel = sourceImage[imgX, imgY];
                                rSum += pixel.R;
                                gSum += pixel.G;
                                bSum += pixel.B;
                                count++;
                            }
                        }
                    }
                }
            }

            if (count == 0) return new Rgb24(0, 0, 0);

            return new Rgb24(
                (byte)(rSum / count),
                (byte)(gSum / count),
                (byte)(bSum / count)
            );
        }

        private double CalculateErrorWithShape(
            int cellX, int cellY, bool[,] occlusionMask, 
            bool[,] shapeMask, int shapeSize, Rgb24 bgColor, Rgb24 fgColor)
        {
            double error = 0.0;

            int centerX = cellX * cellWidth + cellWidth / 2;
            int centerY = cellY * cellHeight + cellHeight / 2;

            int maskSize = shapeMask.GetLength(0);
            int offset = maskSize / 2;

            // Calculate error for ROI
            for (int dy = 0; dy < maskSize; dy++)
            {
                for (int dx = 0; dx < maskSize; dx++)
                {
                    int imgX = centerX - offset + dx;
                    int imgY = centerY - offset + dy;

                    if (imgX >= 0 && imgX < sourceImage.Width && imgY >= 0 && imgY < sourceImage.Height)
                    {
                        if (!occlusionMask[imgX, imgY])
                        {
                            Rgb24 sourcePixel = sourceImage[imgX, imgY];
                            Rgb24 renderPixel = shapeMask[dx, dy] ? fgColor : bgColor;

                            error += ColorDistance(sourcePixel, renderPixel);
                        }
                    }
                }
            }

            return error;
        }

        private double CalculateErrorForCell(
            int cellX, int cellY, bool[,] occlusionMask, 
            ShapeType shape, int size, Rgb24 bgColor, Rgb24 fgColor)
        {
            double error = 0.0;

            int startX = cellX * cellWidth;
            int startY = cellY * cellHeight;
            int endX = Math.Min(startX + cellWidth, sourceImage.Width);
            int endY = Math.Min(startY + cellHeight, sourceImage.Height);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (!occlusionMask[x, y])
                    {
                        Rgb24 sourcePixel = sourceImage[x, y];
                        error += ColorDistance(sourcePixel, bgColor);
                    }
                }
            }

            return error;
        }

        private double ColorDistance(Rgb24 c1, Rgb24 c2)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            return dr * dr + dg * dg + db * db;
        }

        private void UpdateOcclusionMask(int cellX, int cellY, bool[,] occlusionMask, ShapeType shape, int size)
        {
            bool[,] shapeMask = GetShapeMask(shape, size);
            
            int centerX = cellX * cellWidth + cellWidth / 2;
            int centerY = cellY * cellHeight + cellHeight / 2;

            int maskSize = shapeMask.GetLength(0);
            int offset = maskSize / 2;

            for (int dy = 0; dy < maskSize; dy++)
            {
                for (int dx = 0; dx < maskSize; dx++)
                {
                    if (shapeMask[dx, dy])
                    {
                        int imgX = centerX - offset + dx;
                        int imgY = centerY - offset + dy;

                        if (imgX >= 0 && imgX < sourceImage.Width && imgY >= 0 && imgY < sourceImage.Height)
                        {
                            occlusionMask[imgX, imgY] = true;
                        }
                    }
                }
            }
        }

        // Generate circle mask based on size
        // Size 1: radius = 0.5 cell width
        // Size 2: radius = 1.0 cell width
        private bool[,] GetShapeMask(ShapeType shape, int size)
        {
            if (shape == ShapeType.Circle)
            {
                return GenerateCircleMask(size);
            }

            // Default empty mask
            return new bool[1, 1];
        }

        private bool[,] GenerateCircleMask(int size)
        {
            // Calculate radius in pixels
            // Size 1: radius = cellWidth * 0.5
            // Size 2: radius = cellWidth * 1.0
            // Size 3: radius = cellWidth * 1.5
            double radius = cellWidth * (size * 0.5);

            // Mask size needs to accommodate the full circle
            int maskSize = (int)Math.Ceiling(radius * 2) + 1;
            bool[,] mask = new bool[maskSize, maskSize];

            int center = maskSize / 2;
            double radiusSquared = radius * radius;

            for (int y = 0; y < maskSize; y++)
            {
                for (int x = 0; x < maskSize; x++)
                {
                    double dx = x - center;
                    double dy = y - center;
                    double distSquared = dx * dx + dy * dy;

                    mask[x, y] = distSquared <= radiusSquared;
                }
            }

            return mask;
        }

        // Phase 3: Refine background colors based on final visibility
        private void FinalizeBackgrounds()
        {
            // Create final occlusion mask by rendering all shapes
            bool[,] finalMask = new bool[sourceImage.Width, sourceImage.Height];

            // Render shapes in forward order (0 to 1023)
            for (int cellY = 0; cellY < GridSize; cellY++)
            {
                for (int cellX = 0; cellX < GridSize; cellX++)
                {
                    if (grid[cellX, cellY].Shape != ShapeType.None)
                    {
                        UpdateOcclusionMask(cellX, cellY, finalMask, 
                            grid[cellX, cellY].Shape, grid[cellX, cellY].ShapeSize);
                    }
                }
            }

            // Refine background colors for each cell
            for (int cellY = 0; cellY < GridSize; cellY++)
            {
                for (int cellX = 0; cellX < GridSize; cellX++)
                {
                    grid[cellX, cellY].BackgroundColor = ComputeRefinedBackground(cellX, cellY, finalMask);
                }
            }
        }

        private Rgb24 ComputeRefinedBackground(int cellX, int cellY, bool[,] finalMask)
        {
            long rSum = 0, gSum = 0, bSum = 0;
            int count = 0;

            int startX = cellX * cellWidth;
            int startY = cellY * cellHeight;
            int endX = Math.Min(startX + cellWidth, sourceImage.Width);
            int endY = Math.Min(startY + cellHeight, sourceImage.Height);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    // Only include pixels not covered by shapes
                    if (!finalMask[x, y])
                    {
                        Rgb24 pixel = sourceImage[x, y];
                        rSum += pixel.R;
                        gSum += pixel.G;
                        bSum += pixel.B;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                // Fall back to original background if all pixels are covered
                return grid[cellX, cellY].BackgroundColor;
            }

            return new Rgb24(
                (byte)(rSum / count),
                (byte)(gSum / count),
                (byte)(bSum / count)
            );
        }

        // Render the grid to an image for visualization
        public Image<Rgb24> RenderGrid()
        {
            Image<Rgb24> rendered = new Image<Rgb24>(sourceImage.Width, sourceImage.Height);

            // Fill with white background - done by directly setting pixels
            for (int y = 0; y < rendered.Height; y++)
            {
                for (int x = 0; x < rendered.Width; x++)
                {
                    rendered[x, y] = new Rgb24(255, 255, 255);
                }
            }

            // Render in row-major order (0 to 1023)
            for (int cellY = 0; cellY < GridSize; cellY++)
            {
                for (int cellX = 0; cellX < GridSize; cellX++)
                {
                    RenderCell(rendered, cellX, cellY);
                }
            }

            return rendered;
        }

        private void RenderCell(Image<Rgb24> target, int cellX, int cellY)
        {
            GridCell cell = grid[cellX, cellY];

            int startX = cellX * cellWidth;
            int startY = cellY * cellHeight;
            int endX = Math.Min(startX + cellWidth, target.Width);
            int endY = Math.Min(startY + cellHeight, target.Height);

            // Fill background
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    target[x, y] = cell.BackgroundColor;
                }
            }

            // Render shape if present
            if (cell.Shape != ShapeType.None)
            {
                bool[,] shapeMask = GetShapeMask(cell.Shape, cell.ShapeSize);
                
                int centerX = cellX * cellWidth + cellWidth / 2;
                int centerY = cellY * cellHeight + cellHeight / 2;

                int maskSize = shapeMask.GetLength(0);
                int offset = maskSize / 2;

                for (int dy = 0; dy < maskSize; dy++)
                {
                    for (int dx = 0; dx < maskSize; dx++)
                    {
                        if (shapeMask[dx, dy])
                        {
                            int imgX = centerX - offset + dx;
                            int imgY = centerY - offset + dy;

                            if (imgX >= 0 && imgX < target.Width && imgY >= 0 && imgY < target.Height)
                            {
                                target[imgX, imgY] = cell.ForegroundColor;
                            }
                        }
                    }
                }
            }
        }
    }
}
