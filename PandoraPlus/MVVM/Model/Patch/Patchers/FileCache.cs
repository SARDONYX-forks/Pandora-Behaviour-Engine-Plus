using System.Collections.Generic;
using System.IO;

namespace Pandora.Patch.Patchers;
public class FileCache
{

    private readonly Dictionary<string, FileInfo> pathMap = new();

    public FileInfo GetFile(string path)
    {

        if (!this.pathMap.TryGetValue(path, out FileInfo? fileInfo))
        {
            this.pathMap.Add(path, fileInfo = new FileInfo(path));
        }

        return fileInfo;
    }

    public FileInfo[] GetFiles(DirectoryInfo directory)
    {
        FileInfo[] fileArray = directory.GetFiles();

        for (int i = 0; i < fileArray.Length; i++)
        {
            if (this.pathMap.TryGetValue(fileArray[i].FullName, out FileInfo? fileInfo))
            {
                fileArray[i] = fileInfo;
                continue;
            }
            fileInfo = fileArray[i];
            this.pathMap.Add(fileInfo.FullName, fileInfo);
        }
        return fileArray;
    }
}
