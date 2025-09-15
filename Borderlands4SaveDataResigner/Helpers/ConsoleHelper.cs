using System.Text;
using System.Text.RegularExpressions;

namespace Borderlands4SaveDataResigner.Helpers;

public static partial class ConsoleHelper
{
    /// <summary>
    /// Prints the application header information to the console.
    /// </summary>
    /// <param name="appInfo">The application information containing the name, version, and author details.</param>
    /// <param name="breakLine">An optional string to print as a separator after the header. If <see langword="null"/>, no separator is printed.</param>
    public static void PrintHeader(MyAppInfo appInfo, string? breakLine = null)
    {
        Console.WriteLine($"~~* {appInfo.Name} v{MyAppInfo.Version} by {MyAppInfo.Author} *~~");
        if (breakLine != null) Console.WriteLine(breakLine);
    }

    /// <summary>
    /// Prints a welcome message based on the time of day and includes the username.
    /// </summary>
    /// <param name="breakLine"></param>
    public static void SayHello(string? breakLine = null)
    {
        var currentHour = DateTime.Now.Hour;
        var nickName = Environment.UserName;
        var tod = currentHour switch
        {
            > 5 and < 12 => "Good morning",
            >= 12 and < 18 => "Good afternoon",
            _ => "Good evening"
        };
        var greeting = $@"{tod}, {nickName}! \(^-^)/";
        Console.WriteLine(greeting);
        if (breakLine != null) Console.WriteLine(breakLine);
    }

    /// <summary>
    /// Prints a goodbye message based on the time of day and includes the username.
    /// </summary>
    /// /// <param name="breakLine"></param>
    public static void SayGoodbye(string? breakLine = null)
    {
        var currentHour = DateTime.Now.Hour;
        var nickName = Environment.UserName;
        var tod = currentHour switch
        {
            > 5 and < 12 => "Have a great day",
            >= 12 and < 18 => "Enjoy your afternoon",
            _ => "Good night"
        };
        Console.WriteLine($"{tod}, {nickName}! (^-^)");
        if (breakLine != null) Console.WriteLine(breakLine);
    }

    /// <summary>
    /// Prints a Press Any Key To Exit message.
    /// </summary>
    public static void PressAnyKeyToExit()
    {
        Console.Write("Press any key to EXIT...");
        Console.ReadKey();
    }

    /// <summary>
    /// Displays the countdown and closes the application after it reach 0.
    /// </summary>
    /// <param name="countdown">A number of seconds</param>
    /// <param name="msg"></param>
    public static void ExitCountdown(int countdown = 5, string msg = "Application will be closed in")
    {
        var cdLength = countdown.ToString().Length;
        for (var i = countdown; i > 0; i--)
        {
            Console.Write($"{msg} {i.ToString($"D{cdLength}")}.");
            Thread.Sleep(1000);
            Console.CursorLeft = 0;
        }
        Console.Write($"{msg} 0.");
    }

    /// <summary>
    /// Parses a sequence of command-line arguments into a dictionary of key-value pairs.
    /// </summary>
    /// <param name="args">A span of strings representing the command-line arguments. Each argument starting with a single '-'  is treated
    /// as a key, and the subsequent argument (if present and not another key) is treated as its value.</param>
    /// <returns>A dictionary where keys are argument names (prefixed with '-') and values are the associated argument values. If
    /// a key does not have an associated value, its value in the dictionary will be an empty string.</returns>
    public static Dictionary<string, string> ReadArguments(Span<string> args)
    {
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        const char p = '-';
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Length == 0 || arg[0] != p || (arg[0] == p && arg[1] == p)) continue;

            string? value = null;

            if (i + 1 < args.Length)
            {
                var nextArg = args[i + 1];
                if (nextArg.Length > 0)
                {
                    if (nextArg[0] != p || (nextArg.Length > 1 && nextArg[1] == p))
                    {
                        value = nextArg.Length > 1 && nextArg[0] == p && nextArg[1] == p ? nextArg[1..] : nextArg;
                        i++;
                    }
                }
            }
            arguments[arg] = value ?? string.Empty;
        }
        return arguments;
    }

    /// <summary>
    /// Writes the provided dictionary of arguments to the console in a formatted manner.
    /// </summary>
    /// <param name="arguments">A dictionary containing argument names as keys and their corresponding values as values.</param>
    public static void WriteArguments(Dictionary<string, string> arguments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Parsed Arguments:");
        foreach (var kvp in arguments)
            sb.AppendLine($"{kvp.Key}: {kvp.Value}");
        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Creates a string based on the <paramref name="pattern"/> of a given <paramref name="length"/>.
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string StringPattern(string pattern, int length)
    {
        var sb = new StringBuilder();
        while (sb.Length < length)
            sb.Append(pattern);
        return sb.ToString()[..length];
    }

    /// <summary>
    /// Creates a string consisting of the specified character repeated for the specified length.
    /// </summary>
    /// <param name="character">The character to repeat in the resulting string.</param>
    /// <param name="length">The number of times the <paramref name="character"/> is repeated. Must be non-negative.</param>
    /// <returns>A string containing the <paramref name="character"/> repeated <paramref name="length"/> times.</returns>
    public static string StringPattern(char character, int length)
        => new(character, length);

    /// <summary>
    /// Repeats the specified text a given number of times and returns the concatenated result.
    /// </summary>
    /// <param name="text">The text to be repeated.</param>
    /// <param name="times">The number of times to repeat the text. Must be greater than or equal to 0.</param>
    public static void Say(string text, int times = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < times; i++)
            sb.Append(text);
        Console.WriteLine(sb.ToString());
    }

    /// <summary>
    /// Prints a string <paramref name="text"/> in the middle of the line <paramref name="length"/>.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="length"></param>
    public static void SayMiddle(string text, int length)
    {
        // when the console text gets colored, its length become longer then the unformatted string
        var uncolorizedText = UncolorizeString(text);
        string result;
        if (uncolorizedText.Length < length)
        {
            var padding = (int)Math.Floor((decimal)(length - uncolorizedText.Length) / 2);
            var paddingString = StringPattern(' ', padding);
            result = $"{paddingString}{text}{paddingString}";
        }
        else
            result = text;
        Console.WriteLine(result);
    }

    /// <summary>
    /// Prints a string <paramref name="text"/> aligned to the end of the line.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="length"></param>
    public static void SayRight(string text, int length)
    {
        // when the console text gets colored, its length become longer then the unformatted string
        var uncolorizedText = UncolorizeString(text);
        var result = uncolorizedText.Length >= length ? text : $"{StringPattern(' ', length - uncolorizedText.Length)}{text}";
        Console.WriteLine(result);
    }

    /// <summary>
    /// Remove color codes from the <paramref name="str"/> string.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static string UncolorizeString(string str) 
        => ColorFormatRegex().Replace(str, "");
    [GeneratedRegex(@"(\u001b\[.+?m)")]
    private static partial Regex ColorFormatRegex();
}