using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

                    //TEMP
                    Image<Rgb24> preview = pixelated.Preview();
                    Image<Rgb24> generated = preview.GenImage(processedImage);
                    Console.WriteLine($"image rmse: {ImageHelper.Rmse(preview, processedImage)}, shape rmse: {ImageHelper.Rmse(generated, processedImage)}");
                    generated.DisplayImage();
                    Console.WriteLine("Press 'p' to start or pause and 'r' to restart.");

                    for (int y = 0; y < pixelated.Height; y++)
                    {
                        for (int x = 0; x < pixelated.Width; x++)
                        {
                            Rgb24 pixelColor = RoundColor(pixelated[x, y]);

                            PixelToDrawList.Add(new PixelToDraw(pixelColor, new Vector2(x, y)));
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
                            Thread.Sleep(50);
                        }
                        if (Restart)
                        {
                            break;
                        }
                        DrawPixel(item.color, new Vector2((int)Math.Round(item.point.X * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.X), (int)Math.Round(item.point.Y * CoordinateReader.PointOffset + CoordinateReader.FirstPoint.Y)));
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
