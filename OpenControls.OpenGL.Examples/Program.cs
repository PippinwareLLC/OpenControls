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
        bool isMacOs = OperatingSystem.IsMacOS();

        NativeWindowSettings windowSettings = new()
        {
            Size = new Vector2i(1280, 720),
            Title = "OpenControls OpenGL Examples",
            Profile = isMacOs ? ContextProfile.Any : ContextProfile.Compatability,
            APIVersion = isMacOs ? new Version(2, 1) : new Version(3, 3),
            WindowBorder = WindowBorder.Resizable
        };

        using OpenGlExamplesWindow window = new(gameSettings, windowSettings);
        window.Run();
    }
}
