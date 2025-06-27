using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SharpHook;
using SharpHook.Data;

using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace StarvingArtistScript
{
    class Program
    {
        static EventSimulator Simulator = new();
        static Rgb24 curruntColor = new Rgb24(0, 0, 0);

        /*
         * change the coordinate if your resolution is not 2048*1152
         */
        static float DisplayScaling = 1.5f; // 150%
        static Vector2 NewColor = new Vector2(1280,1060);
        static Vector2 NewColorText = new Vector2(1280,940);
        static Vector2 FirstPoint = new Vector2(745, 230);
        static Vector2 LastPoint = new Vector2(1500, 990);
        static float PointOffset = (float)(LastPoint.X - FirstPoint.X) / 31.0f; 
        static int Wait = 50;

        static List<PixelToDraw> PixelToDrawList = new();
        static bool Prompt = true;
        static bool Paused = true;
        static bool Restart = false;
        static SimpleGlobalHook Hook = new SimpleGlobalHook();
        static void Main(string[] args)
        {
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
                else if (e.Data.KeyCode == KeyCode.VcR) {
                    Restart = !Restart;
                    Console.WriteLine("restarting...");
                    Thread.Sleep(500);
                }
            };
            Task.Run(() => Hook.Run());

            while (true) {
                Paused = true;
                Restart = false;
                Prompt = true;
                PixelToDrawList = new List<PixelToDraw>();

                Console.WriteLine("Enter file path...");
                string? fileName = Console.ReadLine();
                if (fileName == null)
                {
                    Console.WriteLine("Error: No input was provided or end of input stream reached.");
                    continue;
                } else {
                    Prompt = false;
                    Console.WriteLine("Press 'p' to start or pause and 'r' to restart.");
                }

                Image<Rgb24> image = Resize(Crop1to1(Image.Load<Rgb24>(fileName)));

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        Rgb24 pixelColor = RoundColor(image[x, y]);

                        PixelToDrawList.Add(new PixelToDraw(pixelColor, new Vector2(x,y)));
                    }
                }

                PixelToDrawList.Sort((e1, e2) => { return e2.color.ToString().CompareTo(e1.color.ToString()); });
                foreach (var item in PixelToDrawList)
                {
                    while (Paused)
                    {
                        if (Restart)
                        {
                            break;
                        }
                    }
                    if (Restart)
                    {
                        break;
                    }
                    DrawPixel(item.color, new Vector2((int)Math.Round(item.point.X * PointOffset + FirstPoint.X), (int)Math.Round(item.point.Y * PointOffset + FirstPoint.Y)));
                }
            }
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

        public static Image<Rgb24> Resize(Image<Rgb24> image)
        {
            return image.Clone(ctx => ctx.Resize(new ResizeOptions{ Size = new Size(32, 32), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.Bicubic }));
        }

        static Rgb24 RoundColor(Rgb24 color, int nearest = 16)
        {
            if (nearest <= 0 || nearest > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(nearest), "Rounding step must be between 1 and 255.");
            }
            byte RoundComponent(byte component, int step)
            {
                float divided = (float)component / step;
                float roundedValue = (float)Math.Round(divided) * step;

                return (byte)Math.Clamp(roundedValue, 0, 255);
            }

            byte r = RoundComponent(color.R, nearest);
            byte g = RoundComponent(color.G, nearest);
            byte b = RoundComponent(color.B, nearest);
            return new Rgb24(r, g, b);
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
            short realX = (short)Math.Round(x / DisplayScaling);
            short realY = (short)Math.Round(y / DisplayScaling);
            RecognisedMouseMovement(realX, realY);

            Simulator.SimulateMousePress(MouseButton.Button1);
            Thread.Sleep(Wait);
            Simulator.SimulateMouseRelease(MouseButton.Button1);
            Thread.Sleep(Wait);
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
                    Click((short)NewColor.X, (short)(NewColor.Y));
                    Click((short)NewColorText.X, (short)NewColorText.Y);
                    string colorString = tmpColor.ToHex();
                    for (int i = 0; i < 6; i++)
                    {
                        SimulateChar(colorString[i]);
                    }
                    Thread.Sleep(Wait);
                    Click((short)NewColor.X, (short)(NewColor.Y));
                }
                Click((short)pos.X, (short)pos.Y);
            }
        }
    }
}
