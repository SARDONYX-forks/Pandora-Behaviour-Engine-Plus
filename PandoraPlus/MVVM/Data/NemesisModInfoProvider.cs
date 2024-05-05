using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Pandora.Core;

namespace Pandora.MVVM.Data;

public interface IModInfoProvider
{
    public Task<List<IModInfo>> GetInstalledMods(string folderPath);
}

public class NemesisModInfoProvider : IModInfoProvider
{
    public async Task<List<IModInfo>> GetInstalledMods(string folderPath)
    {
        return await Task.Run(() => GetInstalledMods(new DirectoryInfo(folderPath)));
    }

    public static List<IModInfo> GetInstalledMods(DirectoryInfo folder)
    {
        List<IModInfo> infoList = new();
        if (!folder.Exists) { return infoList; }

        List<FileInfo> infoFiles = new();
        DirectoryInfo[] modFolders = folder.GetDirectories();

        foreach (DirectoryInfo modFolder in modFolders)
        {
            FileInfo infoFile = new(Path.Join(modFolder.FullName, "info.ini"));
            if (!infoFile.Exists)
            {
                continue;
            }
            infoFiles.Add(infoFile);
        }

        foreach (FileInfo file in infoFiles)
        {
            if (file.Directory == null)
            {
                continue;
            }

            infoList.Add(NemesisModInfo.ParseMetadata(file));
        }
        return infoList;
    }
}
