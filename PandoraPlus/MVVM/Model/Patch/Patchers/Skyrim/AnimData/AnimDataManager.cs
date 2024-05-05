using System.Collections.Generic;
using System.IO;
using Pandora.Core.Patchers.Skyrim;

namespace Pandora.Patch.Patchers.Skyrim.AnimData;

public class AnimDataManager
{

    private static readonly string ANIMDATA_FILENAME = "animationdatasinglefile.txt";

    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly HashSet<int> usedClipIDs = new();
    public int numClipIDs { get; private set; } = 0;

    private readonly List<string> projectNames = new();

    private List<ProjectAnimData> animDataList { get; set; } = new List<ProjectAnimData>();
    private List<MotionData> motionDataList { get; set; } = new List<MotionData>();

    private readonly DirectoryInfo templateFolder;
    private readonly DirectoryInfo outputFolder;

    private FileInfo templateAnimDataSingleFile { get; set; }

    private FileInfo outputAnimDataSingleFile { get; set; }

    private int LastID { get; set; } = 32767;

    public AnimDataManager(DirectoryInfo templateFolder, DirectoryInfo outputFolder)
    {
        this.templateFolder = templateFolder;
        this.outputFolder = outputFolder;
        this.templateAnimDataSingleFile = new FileInfo($"{templateFolder.FullName}\\{ANIMDATA_FILENAME}");
        this.outputAnimDataSingleFile = new FileInfo($"{outputFolder.FullName}\\{ANIMDATA_FILENAME}");
    }

    private void MapProjectAnimData(ProjectAnimData animData)
    {
        foreach (ClipDataBlock block in animData.Blocks)
        {
            _ = this.usedClipIDs.Add(int.Parse(block.ClipID));
        }
    }

    private void MapAnimData()
    {
        foreach (ProjectAnimData animData in this.animDataList)
        {
            this.MapProjectAnimData(animData);
        }
    }

    public int GetNextValidID()
    {
        while (this.usedClipIDs.Contains(this.LastID))
        {
            this.LastID--;
        }
        _ = this.usedClipIDs.Add(this.LastID);
        return this.LastID;
    }

    public void SplitAnimationDataSingleFile(ProjectManager projectManager)
    {
        this.LastID = 32767;

        int NumProjects;

        using (FileStream readStream = this.templateAnimDataSingleFile.OpenRead())
        {
            using StreamReader reader = new(readStream);
            string? expectedLine;
            int projectIndex = 0;
            int sectionIndex = 0;
            NumProjects = int.Parse(reader.ReadLine()!);
            Project? activeProject = null;
            ProjectAnimData? animData = null;
            MotionData motionData;
            while ((expectedLine = reader.ReadLine()) != null)
            {

                if (expectedLine.Contains(".txt"))
                {
                    this.projectNames.Add(Path.GetFileNameWithoutExtension(expectedLine));

                }
                else if (int.TryParse(expectedLine, out int numLines))
                {
                    string projectName = this.projectNames[projectIndex].ToLower();
                    if (projectManager.ProjectLoaded(projectName))
                    {
                        activeProject = projectManager.LookupProject(projectName);
                    }

                    sectionIndex++;

                    if (sectionIndex % 2 != 0)
                    {

                        //using (StreamWriter writer = new StreamWriter(OutputFolder + "\\" + ProjectOrder[i]))
                        //{

                        animData = ProjectAnimData.ReadProject(reader, numLines, this);
#if DEBUG
                        //var outputFile = new FileInfo(AnimDataProjectOutputFolder.FullName + $"\\{ProjectNames[projectIndex]}");
                        //if (outputFile.Exists) outputFile.Delete();
                        //using (var outputWriteStream = outputFile.OpenWrite())
                        //{
                        //	using (var writer = new StreamWriter(outputWriteStream))
                        //	{
                        //		writer.Write(animData.ToString());
                        //	}
                        //}
#endif
                        if (animData.Header.HasMotionData == 0)
                        {
                            projectIndex++;
                            sectionIndex++;
                        }
                        this.animDataList.Add(animData);
                        if (activeProject != null)
                        {
                            activeProject.AnimData = animData;
                        }

                        //writer.Write(project.ToString());
                        //}
                    }
                    else
                    {

                        motionData = MotionData.ReadProject(reader, numLines);
                        if (animData != null)
                        {
                            animData.BoundMotionDataProject = motionData;
                        }

                        this.motionDataList.Add(motionData);
#if DEBUG
                        //var outputFile = new FileInfo(MotionDataProjectOutputFolder.FullName + $"\\{ProjectNames[projectIndex]}");
                        //if (outputFile.Exists) outputFile.Delete();
                        //using (var outputWriteStream = outputFile.OpenWrite())
                        //{
                        //	using (var writer = new StreamWriter(outputWriteStream))
                        //	{
                        //		writer.Write(motionData.ToString());
                        //	}
                        //}
#endif
                        projectIndex++;
                    }
                }
            }
        }
        this.MapAnimData();
    }

    public void MergeAnimDataSingleFile()
    {
        if (this.outputAnimDataSingleFile.Exists) { this.outputAnimDataSingleFile.Delete(); }

        using FileStream writeStream = this.outputAnimDataSingleFile.OpenWrite();
        using StreamWriter streamWriter = new(writeStream);
        streamWriter.WriteLine(this.projectNames.Count);
        foreach (string projectName in this.projectNames) { streamWriter.WriteLine($"{projectName}.txt"); }

        for (int i = 0; i < this.projectNames.Count; i++)
        {
            ProjectAnimData animData = this.animDataList[i];
            MotionData? motionData = animData.BoundMotionDataProject;

            streamWriter.WriteLine(animData.GetLineCount());
            streamWriter.WriteLine(animData.ToString());

            if (motionData == null)
            {
                continue;
            }

            streamWriter.WriteLine(motionData.GetLineCount());
            streamWriter.WriteLine(motionData.ToString());
        }

    }
}
