using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SharpHook;
using SharpHook.Data;

namespace StarvingArtistsScript
{
    class Program
    {
        static EventSimulator Simulator = new();
        static Rgb24 curruntColor = new Rgb24(0, 0, 0);
        static int Wait = 50;
        static int RoundValue = 16;

        static List<PixelToDraw> PixelToDrawList = new();
        static bool Prompt = true;
        static bool Paused = true;
        static bool Restart = false;
        public static SimpleGlobalHook Hook = new SimpleGlobalHook();
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-w" && i + 1 < args.Length && int.TryParse(args[i + 1], out var waitVal))
                {
                    Wait = waitVal;
                    i++;
                }
                else if (args[i] == "-r" && i + 1 < args.Length && int.TryParse(args[i + 1], out var roundVal))
                {
                    RoundValue = roundVal;
                    i++;
                }
            }

            if (args.Length > 0)
            {
                Console.WriteLine($"Wait = {Wait} ms");
                Console.WriteLine($"RoundValue = {RoundValue}");
            }

            DpiHelper.MakeDpiAware();
            Task.Run(() => Hook.Run());
            Hook.KeyPressed += (_, e) =>
            {
                if (Prompt)
                    return;

                if (e.Data.KeyCode == KeyCode.VcP)
                {
                    Paused = !Paused;
                    if (Paused)
                    {
                        Console.WriteLine("Paused...");
                    }
                    else if (!Paused)
                    {
                        Console.WriteLine("Resumed...");
                    }
                    Thread.Sleep(500);
                }
                else if (e.Data.KeyCode == KeyCode.VcR)
                {
                    Restart = !Restart;
                    Console.WriteLine("Restarting...");
                    Thread.Sleep(500);
                }
            };

            bool firstTime = false; //TEMP
            // bool firstTime = true;
            while (true)
            {
                try
                {
                    if (firstTime)
                    {
                        CoordinateReader.FindCoord();
                        firstTime = false;
                    }

                    Paused = true;
                    Restart = false;
                    Prompt = true;
                    PixelToDrawList = new List<PixelToDraw>();

                    Console.WriteLine("Enter file path...");
                    string? fileName = Console.ReadLine();
                    if (fileName == null)
                    {
                        throw new ArgumentNullException("No input was provided or end of input stream reached.");
                    }
                    else
                    {
                        Prompt = false;
                    }
                    Image<Rgb24> processedImage = Image.Load<Rgb24>(fileName).Preprocess();
                    Image<Rgb24> pixelated = processedImage.Pixelate();

                    //TEMP - Preview and optimization
                    Image<Rgb24> preview = pixelated.Preview();
                    Console.WriteLine("Optimizing shapes...");
                    List<ShapeInfo> shapes = ShapeOptimizer.OptimizeShapes(processedImage, pixelated);
                    
                    // Render the optimized image for preview
                    Image<Rgb24> generated = RenderShapes(shapes, processedImage.Width);
                    Console.WriteLine($"image rmse: {ImageHelper.Rmse(preview, processedImage)}, shape rmse: {ImageHelper.Rmse(generated, processedImage)}");
                    generated.DisplayImage();
                    Console.WriteLine("Press 'p' to start or pause and 'r' to restart.");

                    // Draw shapes in rendering order
                    foreach (var shape in shapes)
                    {
                        while (Paused)
                        {
                            if (Restart)
                            {
                                break;
                            }
                            Thread.Sleep(50);
                        }
                        if (Restart)
                        {
                            break;
                        }
                        DrawShape(shape);
                    }
                    Console.WriteLine("Done! Restarting...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An unexpected error occurred. " + ex.Message);
                    Console.WriteLine("Restarting...");
                }
            }
        }

        public static Rgb24 RoundColor(Rgb24 color)
        {
            if (Program.RoundValue <= 0 || Program.RoundValue > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(Program.RoundValue), "Rounding step must be between 1 and 255.");
            }
            byte RoundComponent(byte component, int step)
            {
                float divided = (float)component / step;
                float roundedValue = MathF.Round(divided) * step;

                return (byte)Math.Clamp(roundedValue, 0, 255);
            }

            byte r = RoundComponent(color.R, Program.RoundValue);
            byte g = RoundComponent(color.G, Program.RoundValue);
            byte b = RoundComponent(color.B, Program.RoundValue);
            return new Rgb24(r, g, b);
        }

        public static Image<Rgb24> RenderShapes(List<ShapeInfo> shapes, int size)
        {
            Image<Rgb24> result = new Image<Rgb24>(size, size);
            float pixelSize = (float)size / 32.0f;

            // Render in order (shapes are already in rendering order 0-1023)
            foreach (var shape in shapes)
            {
                // Draw background color for this cell
                int x = (int)shape.Position.X;
                int y = (int)shape.Position.Y;
                
                int xMin = (int)Math.Floor(x * pixelSize);
                int yMin = (int)Math.Floor(y * pixelSize);
                int xMax = (int)Math.Ceiling((x + 1) * pixelSize) - 1;
                int yMax = (int)Math.Ceiling((y + 1) * pixelSize) - 1;

                xMin = Math.Max(0, xMin);
                yMin = Math.Max(0, yMin);
                xMax = Math.Min(size - 1, xMax);
                yMax = Math.Min(size - 1, yMax);

                // Fill background
                for (int py = yMin; py <= yMax; py++)
                {
                    for (int px = xMin; px <= xMax; px++)
                    {
                        result[px, py] = shape.BackgroundColor;
                    }
                }

                // Draw shape if not None
                if (shape.Type != ShapeType.None)
                {
                    float centerX = (shape.Position.X + 0.5f) * pixelSize;
                    float centerY = (shape.Position.Y + 0.5f) * pixelSize;
                    float radius = pixelSize / 2.0f * shape.Size;

                    IPath shapePath = new EllipsePolygon(centerX, centerY, radius);
                    result.Mutate(ctx => ctx.Fill(shape.ForegroundColor, shapePath));
                }
            }

            return result;
        }

        public static void SimulateChar(char c)
        {
            KeyCode key;
            bool shift = false;
            if (c >= 'A' && c <= 'Z')
            {
                key = (KeyCode)((int)KeyCode.VcA + (c - 'A'));
                shift = true;
            }
            else if (c >= '0' && c <= '9')
                key = (KeyCode)((int)KeyCode.Vc0 + (c - '0'));
            else
                throw new NotSupportedException($"Unsupported character: '{c}'");

            if (shift)
                Simulator.SimulateKeyPress(KeyCode.VcLeftShift);

            Simulator.SimulateKeyPress(key);
            Simulator.SimulateKeyRelease(key);

            if (shift)
                Simulator.SimulateKeyRelease(KeyCode.VcLeftShift);
        }

        public static void RecognisedMouseMovement(short x, short y, int steps = 5, int delayMs = 5)
        {
            Simulator.SimulateMouseMovement(x, (short)(y + steps));
            Thread.Sleep(delayMs);
            Simulator.SimulateMouseMovement((short)(x + steps), y);
            Thread.Sleep(delayMs);
            Simulator.SimulateMouseMovement((short)(x - steps), y);
            Thread.Sleep(delayMs);
            Simulator.SimulateMouseMovement(x, (short)(y - steps));
            Thread.Sleep(delayMs);
            Simulator.SimulateMouseMovement(x, y);
            Thread.Sleep(Wait);
        }

        static void Click(short x, short y)
        {
            RecognisedMouseMovement(x, y);

            Simulator.SimulateMousePress(MouseButton.Button1);
            Thread.Sleep(Wait);
            Simulator.SimulateMouseRelease(MouseButton.Button1);
            Thread.Sleep(Wait);
        }

        static void ClickAndDrag(short x1, short y1, short x2, short y2)
        {
            RecognisedMouseMovement(x1, y1);
            
            Simulator.SimulateMousePress(MouseButton.Button1);
            Thread.Sleep(Wait);
            
            RecognisedMouseMovement(x2, y2);
            Thread.Sleep(Wait);
            
            Simulator.SimulateMouseRelease(MouseButton.Button1);
            Thread.Sleep(Wait);
        }

        static void DrawShape(ShapeInfo shape)
        {
            if (shape.Type == ShapeType.None)
            {
                // Just paint background color
                DrawPixel(shape.BackgroundColor, new Vector2(
                    (int)Math.Round(shape.Position.X * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.X),
                    (int)Math.Round(shape.Position.Y * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.Y)
                ));
                return;
            }

            // 1. Click shape tool icon to select it
            Click((short)CoordinateReader.ShapeTool.X, (short)CoordinateReader.ShapeTool.Y);

            // 2. Set foreground color (shape color)
            Rgba32 tmpFgColor = new Rgba32();
            shape.ForegroundColor.ToRgba32(ref tmpFgColor);
            
            Click((short)CoordinateReader.NewColor.X, (short)CoordinateReader.NewColor.Y);
            Click((short)CoordinateReader.NewColorText.X, (short)CoordinateReader.NewColorText.Y);
            string fgColorString = tmpFgColor.ToHex();
            for (int i = 0; i < 6; i++)
            {
                SimulateChar(fgColorString[i]);
            }
            Thread.Sleep(Wait);
            Click((short)CoordinateReader.NewColor.X, (short)CoordinateReader.NewColor.Y);

            // 3. Calculate center position in screen coordinates
            float centerX = (shape.Position.X + 0.5f) * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.X;
            float centerY = (shape.Position.Y + 0.5f) * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.Y;

            // 4. Calculate drag distance based on shape size
            // Size 1 = radius 0.5 cells, Size 2 = radius 1 cell, Size 3 = radius 1.5 cells
            float radiusInCells = shape.Size * 0.5f;
            float radiusInPixels = radiusInCells * CoordinateReader.PointOffset;

            // Click and drag from center to define the circle size
            short x1 = (short)Math.Round(centerX);
            short y1 = (short)Math.Round(centerY);
            short x2 = (short)Math.Round(centerX + radiusInPixels);
            short y2 = (short)Math.Round(centerY);

            ClickAndDrag(x1, y1, x2, y2);

            // 5. Click close button
            Click((short)CoordinateReader.CloseButton.X, (short)CoordinateReader.CloseButton.Y);

            // 6. Now paint the background color if needed (paint the cell background)
            DrawPixel(shape.BackgroundColor, new Vector2(
                (int)Math.Round(shape.Position.X * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.X),
                (int)Math.Round(shape.Position.Y * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.Y)
            ));
        }

        static void DrawPixel(Rgb24 color, Vector2 pos)
        {
            // Ignore white pixels
            Rgba32 tmpColor = new Rgba32();
            color.ToRgba32(ref tmpColor);
            if (tmpColor.ToHex() != "FFFFFFFF")
            {
                if (curruntColor != color)
                {
                    curruntColor = color;
                    Click((short)CoordinateReader.NewColor.X, (short)(CoordinateReader.NewColor.Y));
                    Click((short)CoordinateReader.NewColorText.X, (short)CoordinateReader.NewColorText.Y);
                    string colorString = tmpColor.ToHex();
                    for (int i = 0; i < 6; i++)
                    {
                        SimulateChar(colorString[i]);
                    }
                    Thread.Sleep(Wait);
                    Click((short)CoordinateReader.NewColor.X, (short)(CoordinateReader.NewColor.Y));
                }
                Click((short)pos.X, (short)pos.Y);
            }
        }
    }
}
