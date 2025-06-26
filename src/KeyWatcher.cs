using SharpHook.Data;
using SharpHook;

namespace StarvingArtistScript {
    public class KeyWatcher
    {
        private static HashSet<KeyCode> keysPressed = new();

        public static void Start()
        {
            var hook = new SimpleGlobalHook();
            hook.KeyPressed += (_, e) => keysPressed.Add(e.Data.KeyCode);
            hook.KeyReleased += (_, e) => keysPressed.Remove(e.Data.KeyCode);
            hook.Run();
        }

        public static bool IsKeyDown(KeyCode code) => keysPressed.Contains(code);
    }
}
