using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Borderlands4SaveDataResigner.Helpers;

public static class UrlHelpers
{
    /// <summary>
    /// Opens the given URL in the default web browser.
    /// </summary>
    /// <param name="url"></param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static void OpenUrl(this string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            throw new PlatformNotSupportedException("Unsupported OS platform.");
    }
}