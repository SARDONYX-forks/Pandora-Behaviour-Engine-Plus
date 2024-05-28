using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Pandora.Core;

namespace Pandora.MVVM.Data;
public class PandoraModInfoProvider : IModInfoProvider
{
    public async Task<List<IModInfo>> GetInstalledMods(string folderPath)
    {
        return await Task.Run(() => GetInstalledMods(new DirectoryInfo(folderPath)));
    }

    private static readonly XmlSerializer xmlSerializer = new(typeof(PandoraModInfo));
    public static List<IModInfo> GetInstalledMods(DirectoryInfo folder)
    {
        List<IModInfo> infoList = new();
        if (!folder.Exists) { return infoList; }

        List<FileInfo> infoFiles = new();
        DirectoryInfo[] modFolders = folder.GetDirectories();

        foreach (DirectoryInfo modFolder in modFolders)
        {
            //var files = modFolder.GetFiles("info.xml");
            FileInfo infoFile = new(Path.Join(modFolder.FullName, "info.xml"));
            if (!infoFile.Exists) { continue; }
            infoFiles.Add(infoFile);
        }

        foreach (FileInfo file in infoFiles)
        {
            if (file.Directory == null)
            {
                continue;
            }

            using FileStream readStream = file.OpenRead();
            using XmlReader xmlReader = XmlReader.Create(readStream);
            object? modInfoObj = xmlSerializer.Deserialize(xmlReader);
            if (modInfoObj == null) { continue; }

            PandoraModInfo modInfo = (PandoraModInfo)modInfoObj;
            modInfo.FillData(file.Directory);
            infoList.Add(modInfo);

        }
        return infoList;
    }
}
