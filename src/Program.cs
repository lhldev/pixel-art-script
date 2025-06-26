using System.Numerics;
using System.Runtime.InteropServices;
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
        static EventSimulator simulator = new();
        static Rgb24 curruntColor = new Rgb24(0, 0, 0);

        /*
         * change the coordinate if your resolution is not 2048*1152
         */
        static Vector2 newColor = new Vector2(1156,873);
        static Vector2 newColorType = new Vector2(1153,795);
        static int[] pointsX = { 711, 729, 750, 770, 791, 811, 832, 851, 871, 892, 914, 932, 952, 974, 992, 1014, 1034, 1054, 1074, 1093, 1116, 1139, 1156, 1177, 1196, 1214, 1235, 1257, 1277, 1297, 1319, 1337 };
        static int[] pointsY = { 191, 208, 231, 249, 268, 291, 312, 329, 353, 371, 391, 410, 431, 451, 474, 492, 509, 532, 554, 573, 593, 614, 633, 656, 675, 693, 716, 736, 757, 778, 796, 816 };

        static List<PixelToDraw> PixelToDrawList = new();
        static bool Paused = true;
        static bool Restart = false;
        static Thread pauserThread = new Thread(new ThreadStart(pauser));
        static void Main(string[] args)
        {
            while (true) {
                Init();
                Console.WriteLine("enter file name...");

                string? fileName = Console.ReadLine();
                if (fileName == null)
                {
                    Console.WriteLine("Error: No input was provided or end of input stream reached.");
                    fileName = "defaultFileName.txt";
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
                PixelToDrawList.Sort((e1, e2) =>
                        {
                        return e2.color.ToString().CompareTo(e1.color.ToString());
                        });
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
                        Console.Clear();
                        Restart = false;
                        break;
                    }
                    DrawPixel(item.color, new Vector2(pointsX[(int)item.point.X], pointsY[(int)item.point.Y]));
                }
            }
        }

        public static void Init()
        {
            if (!pauserThread.IsAlive)
            {
                pauserThread.SetApartmentState(ApartmentState.STA);
                pauserThread.Start();
            }
            PixelToDrawList = new List<PixelToDraw>();
            Paused = true;
            Restart = false;

            KeyWatcher.Start();
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
                double divided = (double)component / step;
                double roundedValue = Math.Round(divided) * step;

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
                simulator.SimulateKeyPress(KeyCode.VcLeftShift);

            simulator.SimulateKeyPress(key);
            simulator.SimulateKeyRelease(key);

            if (shift)
                simulator.SimulateKeyRelease(KeyCode.VcLeftShift);
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
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y + 16), false);
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y + 4), false);
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y), true);
                    Click.click(new System.Drawing.Point((int)newColorType.X, (int)newColorType.Y), false);
                    Click.click(new System.Drawing.Point((int)newColorType.X + 16, (int)newColorType.Y), true);
                    string colorString = tmpColor.ToHex();
                    for (int i = 0; i < 6; i++)
                    {
                        SimulateChar(colorString[i]);
                    }
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y + 16), false);
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y+4), false);
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y), true);
                }
                Click.click(new System.Drawing.Point((int)pos.X, (int)pos.Y), true);
            }
        }

        public static void pauser()
        {
            while (true)
            {
                if (KeyWatcher.IsKeyDown(KeyCode.VcF1))
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
                else if (KeyWatcher.IsKeyDown(KeyCode.VcF2))
                {
                    Restart = !Restart;
                    Console.WriteLine("restart: " + Restart);
                    Thread.Sleep(500);
                }
            }
        }

        public class Click
        {
            [DllImport("user32.dll")]
            static extern void mouse_event(int dwFlags, int dx, int dy,
                      int dwData, int dwExtraInfo);

            [Flags]
            public enum MouseEventFlags
            {
                LEFTDOWN = 0x00000002,
                LEFTUP = 0x00000004,
                MIDDLEDOWN = 0x00000020,
                MIDDLEUP = 0x00000040,
                MOVE = 0x00000001,
                ABSOLUTE = 0x00008000,
                RIGHTDOWN = 0x00000008,
                RIGHTUP = 0x00000010
            }
            [DllImport("User32.dll",
            EntryPoint = "GetSystemMetrics",
            CallingConvention = CallingConvention.Winapi)]
            internal static extern int InternalGetSystemMetrics(int value);

            public static void click(System.Drawing.Point p, bool willclick)
            {
                // Move mouse cursor to absolute position to_x, to_y and make left button click:
                int to_x = p.X;
                int to_y = p.Y;

                int screenWidth = InternalGetSystemMetrics(0);
                int screenHeight = InternalGetSystemMetrics(1);

                // Mickey X coordinate
                int mic_x = (int)(to_x * 65536.0 / screenWidth);
                // Mickey Y coordinate
                int mic_y = (int)(to_y * 65536.0 / screenHeight);

                // 0x0001 | 0x8000: Move + Absolute position
                mouse_event(0x0001 | 0x8000, mic_x, mic_y, 0, 0);
                Thread.Sleep(20);
                if (willclick == true)
                {
                    mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
                    Thread.Sleep(20);
                    mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
                }
            }

        }
    }
}
