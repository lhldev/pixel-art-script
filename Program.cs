using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Input;
using Cursor = System.Windows.Forms.Cursor;
using System.Security.Cryptography.X509Certificates;
using System.Drawing.Drawing2D;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices.ComTypes;

//restart loop

namespace gamingplatform32bitpainter_starving_artist_
{
    class Program
    {
        static Color curruntColor = Color.Black;
        static Vector newColor = new Vector(1156,877);
        static Vector newColorType = new Vector(1153,795);
        public static int[] pointsX = { 711, 729, 750, 770, 791, 811, 832, 851, 871, 892, 914, 932, 952, 974, 992, 1014, 1034, 1054, 1074, 1093, 1116, 1139, 1156, 1177, 1196, 1214, 1235, 1257, 1277, 1297, 1319, 1337 };
        public static int[] pointsY = { 191, 208, 231, 249, 268, 291, 312, 329, 353, 371, 391, 410, 431, 451, 474, 492, 509, 532, 554, 573, 593, 614, 633, 656, 675, 693, 716, 736, 757, 778, 796, 816 };
        public static List<PixelToDraw> PixelToDrawList = new List<PixelToDraw>();
        public static bool Paused = true;

        static void Main(string[] args)
        {
            Thread pauserThread = new Thread(new ThreadStart(pauser));
            pauserThread.SetApartmentState(ApartmentState.STA);
            pauserThread.Start();
            Console.WriteLine("enter file name...");
            string fileName = Console.ReadLine().Replace("\"", "");
            Bitmap image = Resize32(Crop1to1(new Bitmap(fileName)));

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixelColor = RoundColor(image.GetPixel(x, y));// <-- round color
                    
                    PixelToDrawList.Add(new PixelToDraw(pixelColor, new Vector(x,y)));
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
                    //pause when paused is true
                }
                DrawPixel(item.color, new Vector(pointsX[(int)item.point.X], pointsY[(int)item.point.Y]));
            }

            //repeating
            pauserThread.Abort();
            PixelToDrawList = new List<PixelToDraw>();
            Paused = true;
            Main(args = null);
        }


        public static Bitmap Crop1to1(Bitmap image)
        {
            if (image.Width == image.Height)
            {
                //excape if already 1:1
                return image;
            }

            int width = image.Width;
            int height = image.Height;

            // Determine the smaller dimension
            int minDimension = Math.Min(width, height);

            // Calculate the cropping coordinates
            int x = (width - minDimension) / 2;
            int y = (height - minDimension) / 2;

            // Create a rectangle representing the cropping area
            Rectangle cropArea = new Rectangle(x, y, minDimension, minDimension);

            // Crop the image and return the result
            Bitmap croppedImage = new Bitmap(minDimension, minDimension);
            using (Graphics g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(image, new Rectangle(0, 0, minDimension, minDimension), cropArea, GraphicsUnit.Pixel);
            }
            return croppedImage;
        }

        public static Bitmap Resize32(Bitmap image)
        {
            Bitmap resizedImage = new Bitmap(32, 32);
            using (var graphics = Graphics.FromImage(resizedImage))
            {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; // set interpolation mode
                    graphics.DrawImage(image, 0, 0, 32, 32);
            }
            return resizedImage;
        }

        static Color RoundColor(Color color, int nearest = 16)
        {
            int red = (int)Math.Round((double)(color.R / nearest)) * nearest;
            int green = (int)Math.Round((double)(color.G / nearest)) * nearest;
            int blue = (int)Math.Round((double)(color.B / nearest)) * nearest;
            int alpha = (int)Math.Round((double)(color.A / nearest)) * nearest;
            return Color.FromArgb(alpha, red, green, blue);
        }


        static void DrawPixel(Color color, Vector pos)
        {
            if (ColorTranslator.ToHtml(Color.FromArgb(color.ToArgb())).ToString() != "#FFFFFF")
            {
                if (curruntColor != color)
                {
                    curruntColor = color;
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y + 4), false);
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y), true);
                    Click.click(new System.Drawing.Point((int)newColorType.X, (int)newColorType.Y), false);
                    Click.click(new System.Drawing.Point((int)newColorType.X + 20, (int)newColorType.Y), true);
                    foreach (char c in ColorTranslator.ToHtml(Color.FromArgb(color.ToArgb())).ToString())
                    {
                        SendKeys.SendWait(c.ToString());
                    }
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y+20), false);
                    Click.click(new System.Drawing.Point((int)newColor.X, (int)newColor.Y), true);

                }
                Click.click(new System.Drawing.Point((int)pos.X, (int)pos.Y), true);
            }
        }

        public static void pauser()
        {
            while (true)
            {
                if (Keyboard.IsKeyDown(Key.F1) && Paused == false)
                {
                    Paused = true;
                    Thread.Sleep(500);
                }
                else if (Keyboard.IsKeyDown(Key.F1) && Paused == true)
                {
                    Paused = false;
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
                Thread.Sleep(10);
                if (willclick == true)
                {
                    mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
                    Thread.Sleep(30);
                    mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
                }
            }

        }
    }
}
