using System.IO;

namespace Pandora.Patch.Patchers;

public static class StreamExtensions
{
    public static string ReadLineSafe(this StreamReader reader)
    {
        return reader.ReadLine() ?? string.Empty;
    }
}
