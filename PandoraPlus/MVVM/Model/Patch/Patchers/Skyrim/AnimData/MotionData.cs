using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimData;

public class MotionData
{
    public List<ClipMotionDataBlock> Blocks { get; private set; } = new List<ClipMotionDataBlock>();
    public Dictionary<int, ClipMotionDataBlock> BlocksByID { get; private set; } = new Dictionary<int, ClipMotionDataBlock>();

    public void AddDummyClipMotionData(string id)
    {
        lock (this.Blocks) { this.Blocks.Add(new ClipMotionDataBlock(id)); }
    }
    public static MotionData ReadProject(StreamReader reader, int lineLimit)
    {
        MotionData project = new();
        int i = 1; //+1 to account for 1 empty line 
        string? whiteSpace = "";

        while (whiteSpace != null && i < lineLimit)
        {
            ClipMotionDataBlock block = ClipMotionDataBlock.ReadBlock(reader);
            project.Blocks.Add(block);
            project.BlocksByID.Add(int.Parse(block.ClipID), block);
            i += block.GetLineCount();

            whiteSpace = reader.ReadLine();
            i++;

        }
        return project;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        _ = sb.AppendJoin("\r\n", this.Blocks);
        return sb.ToString();
        //byte[] bytes = Encoding.Default.GetBytes(sb.ToString());
        //return Encoding.UTF8.GetString(bytes);
    }
    public int GetLineCount()
    {
        int i = 0;
        foreach (ClipMotionDataBlock block in this.Blocks)
        {
            i += block.GetLineCount();
            i++;
        }
        return i;
    }

}
