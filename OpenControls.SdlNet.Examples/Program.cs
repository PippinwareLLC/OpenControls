using System.Runtime.InteropServices;

namespace OpenControls.SdlNet.Examples;

public static class Program
{
    public static int Main()
    {
        if (!TryLoadSdl(out string error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        try
        {
            using SdlExamplesApp app = new();
            app.Run();
            return 0;
        }
        catch (DllNotFoundException)
        {
            Console.Error.WriteLine(BuildMissingSdlMessage());
            return 1;
        }
    }

    private static bool TryLoadSdl(out string error)
    {
        error = string.Empty;
        try
        {
            if (!NativeLibrary.TryLoad("SDL2.dll", out IntPtr handle))
            {
                error = BuildMissingSdlMessage();
                return false;
            }

            NativeLibrary.Free(handle);
            return true;
        }
        catch (BadImageFormatException)
        {
            error = "SDL2.dll was found but does not match the process architecture. Install the x64 SDL2 runtime or run an x86 build.";
            return false;
        }
        catch (DllNotFoundException)
        {
            error = BuildMissingSdlMessage();
            return false;
        }
    }

    private static string BuildMissingSdlMessage()
    {
        return "SDL2 native runtime not found. Install SDL2 and ensure SDL2.dll is on PATH or next to the executable.";
    }
}
