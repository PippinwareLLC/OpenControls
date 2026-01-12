using OpenControls.Examples;
using OpenControls.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace OpenControls.OpenGL.Examples;

public static class Program
{
    public static void Main()
    {
        GameWindowSettings gameSettings = GameWindowSettings.Default;
        gameSettings.UpdateFrequency = 60;

        NativeWindowSettings windowSettings = new()
        {
            Size = new Vector2i(1280, 720),
            Title = "OpenControls OpenGL Examples",
            Profile = ContextProfile.Compatability,
            APIVersion = new Version(3, 3),
            WindowBorder = WindowBorder.Resizable
        };

        using OpenGlExamplesWindow window = new(gameSettings, windowSettings);
        window.Run();
    }
}
