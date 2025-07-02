using System.Numerics;
using SharpHook;
using SharpHook.Data;

namespace StarvingArtistsScript
{
    public class CoordinateReader
    {
        public static Vector2 NewColor;
        public static Vector2 NewColorText;
        public static Vector2 FirstPoint;
        public static Vector2 LastPoint;
        public static float PointOffset;

        private static int clickCount = 0;
        private static string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coords.txt");

        public static void FindCoord()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coords.txt");

            if (!File.Exists(filePath))
            {
                Program.Hook.MousePressed += OnMousePressed;

                Console.WriteLine("This is your first time running this script. Please follow the instruction to get the position needed.");
                Console.WriteLine("Double click on the first square (top left of canvas)");
                while (clickCount != 1)
                    Thread.Sleep(50);
                Console.WriteLine("Double click on the last square (bottom right of canvas)");
                while (clickCount != 2)
                    Thread.Sleep(50);
                Console.WriteLine("Double click on the icon to change color (6th icon at the bottom)");
                while (clickCount != 3)
                    Thread.Sleep(50);
                Console.WriteLine("Double click on the text area to change color");
                while (clickCount != 4)
                    Thread.Sleep(50);
                SaveCoords();
            }

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string[] parts = line.Split('=', StringSplitOptions.TrimEntries);
                string key = parts[0];
                string value = parts[1];

                Vector2 parsedVector;
                if (TryParseVector2(value, out parsedVector))
                {
                    switch (key)
                    {
                        case "NewColor":
                            NewColor = parsedVector;
                            break;
                        case "NewColorText":
                            NewColorText = parsedVector;
                            break;
                        case "FirstPoint":
                            FirstPoint = parsedVector;
                            break;
                        case "LastPoint":
                            LastPoint = parsedVector;
                            break;
                        default:
                            throw new InvalidDataException($"Unknown key {key}.");
                    }
                }
                else
                {
                    throw new InvalidDataException($"Unknown value {value}.");
                }
            }

            PointOffset = (float)(LastPoint.X - FirstPoint.X) / 31.0f;
        }

        private static void OnMousePressed(object? sender, MouseHookEventArgs e)
        {
            if (e.Data.Button == MouseButton.Button1 && e.Data.Clicks == 2)
            {
                Vector2 pos = new Vector2(e.Data.X, e.Data.Y);

                switch (clickCount)
                {
                    case 0:
                        FirstPoint = pos;
                        Console.WriteLine($"[1/4] FirstPoint set to {pos}");
                        break;
                    case 1:
                        LastPoint = pos;
                        Console.WriteLine($"[2/4] LastPoint set to {pos}");
                        break;
                    case 2:
                        NewColor = pos;
                        Console.WriteLine($"[3/4] NewColor set to {pos}");
                        break;
                    case 3:
                        NewColorText = pos;
                        Console.WriteLine($"[4/4] NewColorText set to {pos}");
                        break;
                    default:
                        break;
                }

                clickCount++;
            }
        }

        private static void SaveCoords()
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine($"FirstPoint={FirstPoint.X},{FirstPoint.Y}");
                writer.WriteLine($"LastPoint={LastPoint.X},{LastPoint.Y}");
                writer.WriteLine($"NewColor={NewColor.X},{NewColor.Y}");
                writer.WriteLine($"NewColorText={NewColorText.X},{NewColorText.Y}");
            }
        }

        private static bool TryParseVector2(string input, out Vector2 result)
        {
            result = new Vector2();
            string[] components = input.Split(',', StringSplitOptions.TrimEntries);

            int x, y;
            if (components.Length == 2 && int.TryParse(components[0], out x) && int.TryParse(components[1], out y))
            {
                result = new Vector2(x, y);
                return true;
            }
            return false;
        }
    }
}
