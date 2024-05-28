using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimData;

public class ProjectAnimData
{
    public ProjectAnimDataHeader Header { get; private set; } = new ProjectAnimDataHeader();
    public List<ClipDataBlock> Blocks { get; set; } = new List<ClipDataBlock>();

    public MotionData? BoundMotionDataProject { get; set; }

    private AnimDataManager manager { get; set; }

    private HashSet<string> dummyClipNames { get; set; } = new HashSet<string>();
    public ProjectAnimData(AnimDataManager manager)
    {
        this.manager = manager;
    }

    public void AddDummyClipData(string clipName)
    {
        lock (this.dummyClipNames)
        {
            if (this.dummyClipNames.Contains(clipName))
            {
                return;
            }
        }

        string id = this.manager.GetNextValidID().ToString();
        this.Blocks.Add(new ClipDataBlock(clipName, id));

        this.BoundMotionDataProject?.AddDummyClipMotionData(id);
        lock (this.dummyClipNames)
        {
            _ = this.dummyClipNames.Add(clipName);
        }

    }

    public static ProjectAnimData ReadProject(StreamReader reader, int lineLimit, AnimDataManager manager)
    {
        ProjectAnimData project = new(manager)
        {
            Header = ProjectAnimDataHeader.ReadBlock(reader)
        };

        int i = project.Header.GetLineCount() + 1; //+1 to account for 1 empty line 
        string whiteSpace = "";

        while (whiteSpace != null && i < lineLimit)
        {
            ClipDataBlock block = ClipDataBlock.ReadBlock(reader);
            project.Blocks.Add(block);
            i += block.GetLineCount();

            whiteSpace = reader.ReadLine();
            i++;

        }
        return project;
    }
    public static ProjectAnimData ReadProject(StreamReader reader, AnimDataManager manager)
    {
        ProjectAnimData project = new(manager)
        {
            Header = ProjectAnimDataHeader.ReadBlock(reader)
        };

        string whiteSpace = "";
        while (whiteSpace != null)
        {
            ClipDataBlock block = ClipDataBlock.ReadBlock(reader);
            project.Blocks.Add(block);

            whiteSpace = reader.ReadLine();

        }
        return project;
    }
    public static ProjectAnimData ExtractProject(StreamReader reader, string openString, string closeString, AnimDataManager manager)
    {
        while (!(_ = reader.ReadLine()).Contains(openString))
        {

        }
        ProjectAnimData project = new(manager)
        {
            Header = ProjectAnimDataHeader.ReadBlock(reader)
        };

        string whiteSpace = "";
        while (whiteSpace != null && !whiteSpace.Contains(closeString))
        {
            ClipDataBlock block = ClipDataBlock.ReadBlock(reader);
            project.Blocks.Add(block);

            whiteSpace = reader.ReadLineSafe();

        }
        return project;
    }
    public override string ToString()
    {
        StringBuilder sb = new();
        _ = sb.Append(this.Header.ToString());
        if (this.Blocks.Count > 0)
        {
            _ = sb.AppendJoin("\r\n", this.Blocks);
        }
        return sb.ToString();
        //byte[] bytes = Encoding.Default.GetBytes(sb.ToString());
        //return Encoding.UTF8.GetString(bytes);
    }
    public int GetLineCount()
    {
        int i = this.Header.GetLineCount() + 1;
        foreach (ClipDataBlock block in this.Blocks)
        {
            i += block.GetLineCount();
            i++;
        }
        return i;
    }
}
