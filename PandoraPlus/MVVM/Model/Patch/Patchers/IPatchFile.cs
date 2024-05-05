using System.IO;

namespace Pandora.Patch.Patchers;

public interface IPatchFile
{
    public FileInfo InputHandle { get; }

    public FileInfo OutputHandle { get; }

    public bool Export();
}
