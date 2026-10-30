using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LOLFriendsCleaner;

internal static class DarkTitleBar
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int on = 1;
            DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
            DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int)); // valor antigo (builds iniciais do Win10)

            int caption = 0x00140F0D; // COLORREF 0x00BBGGRR -> #0D0F14
            int text = 0x00EEEAE8;    //                         #E8EAEE
            DwmSetWindowAttribute(hwnd, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR (Win11)
            DwmSetWindowAttribute(hwnd, 36, ref text, sizeof(int));    // DWMWA_TEXT_COLOR (Win11)
        }
        catch
        {
        }
    }
}
