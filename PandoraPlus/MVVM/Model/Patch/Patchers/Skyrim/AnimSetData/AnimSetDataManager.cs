using System.Collections.Generic;
using System.IO;

namespace Pandora.Patch.Patchers.Skyrim.AnimSetData;

public class AnimSetDataManager
{
    private static readonly string ANIMSETDATA_FILENAME = "animationsetdatasinglefile.txt";

    private DirectoryInfo templateFolder { get; set; }
    private DirectoryInfo outputFolder { get; set; }
    private FileInfo templateAnimSetDataSingleFile { get; set; }
    private FileInfo outputAnimSetDataSingleFile { get; set; }

    private FileInfo vanillaHkxFiles { get; set; }

    private List<string> projectPaths { get; set; } = new List<string>();

    private List<ProjectAnimSetData> animSetDataList { get; set; } = new List<ProjectAnimSetData>();

    public Dictionary<string, ProjectAnimSetData> AnimSetDataMap { get; private set; } = new Dictionary<string, ProjectAnimSetData>();

    public AnimSetDataManager(DirectoryInfo templateFolder, DirectoryInfo outputFolder)
    {
        this.templateFolder = templateFolder;
        this.outputFolder = outputFolder;
        this.templateAnimSetDataSingleFile = new FileInfo($"{templateFolder.FullName}\\{ANIMSETDATA_FILENAME}");
        this.outputAnimSetDataSingleFile = new FileInfo($"{outputFolder.FullName}\\{ANIMSETDATA_FILENAME}");
        this.vanillaHkxFiles = new FileInfo($"{templateFolder.FullName}\\vanilla_hkxpaths.txt");
    }

    public void SplitAnimSetDataSingleFile()
    {
        using FileStream readStream = this.templateAnimSetDataSingleFile.OpenRead();
        using StreamReader reader = new(readStream);
        int NumProjects = int.Parse(reader.ReadLine()!);
        for (int i = 0; i < NumProjects; i++)
        {
            this.projectPaths.Add(reader.ReadLineSafe());
        }

        for (int i = 0; i < NumProjects; i++)
        {
            ProjectAnimSetData animSetData = ProjectAnimSetData.Read(reader);
            this.animSetDataList.Add(animSetData);
            this.AnimSetDataMap.Add(Path.GetFileNameWithoutExtension(this.projectPaths[i]), animSetData);

            //#if DEBUG
            //						FileInfo animDataFile = new FileInfo($"{outputFolder.FullName}\\animsetdata\\{(Path.GetFileName(projectPaths[i]))}");
            //						if (animDataFile.Exists) { animDataFile.Delete(); }
            //						if (!(animDataFile.Directory!.Exists)) { animDataFile.Directory.Create(); }
            //						using (var stream = animDataFile.OpenWrite())
            //						{
            //							using (var writer = new StreamWriter(stream))
            //							{
            //								writer.Write(animSetDataList[i]);
            //							}
            //						}

            //#endif

        }
    }

    public void MergeAnimSetDataSingleFile()
    {
        if (this.outputAnimSetDataSingleFile.Exists) { this.outputAnimSetDataSingleFile.Delete(); }
        if (this.outputAnimSetDataSingleFile.Directory != null && !this.outputAnimSetDataSingleFile.Directory.Exists) { this.outputAnimSetDataSingleFile.Directory.Create(); }

        using FileStream writeStream = this.outputAnimSetDataSingleFile.OpenWrite();
        using StreamWriter writer = new(writeStream);
        writer.WriteLine(this.projectPaths.Count);
        foreach (string projectPath in this.projectPaths) { writer.WriteLine(projectPath); }
        foreach (ProjectAnimSetData animSetData in this.animSetDataList)
        {
            writer.Write(animSetData);
        }
    }
}
