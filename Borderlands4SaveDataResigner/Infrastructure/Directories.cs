using Borderlands4SaveDataResigner.Helpers;

namespace Borderlands4SaveDataResigner.Infrastructure;

public class Directories
{
    public string Output { get; } = Path.Combine(MyAppInfo.RootPath, "_OUTPUT");

    public void CreateAll()
    {
        Directory.CreateDirectory(Output);
    }
}