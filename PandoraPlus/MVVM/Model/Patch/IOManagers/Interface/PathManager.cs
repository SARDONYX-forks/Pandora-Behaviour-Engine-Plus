using System.IO;

namespace Pandora.Core.IOManagers;

public interface PathManager
{
    public bool Export(FileInfo inFile);

    public DirectoryInfo Import(FileInfo inFile);

}
