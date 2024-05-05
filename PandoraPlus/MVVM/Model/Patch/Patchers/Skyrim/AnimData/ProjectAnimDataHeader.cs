using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimData;

public class ProjectAnimDataHeader
{
    public int LeadInt { get; set; }

    public int AssetCount { get; set; }
    public List<string> ProjectAssets { get; set; } = new List<string>();
    public int HasMotionData { get; set; }

    public static ProjectAnimDataHeader ReadBlock(StreamReader reader)
    {
        ProjectAnimDataHeader header = new();
        try
        {
            int[] headerData = new int[0];

            header.LeadInt = int.Parse(reader.ReadLine());

            header.AssetCount = int.Parse(reader.ReadLine());

            for (int i = 0; i < header.AssetCount; i++)
            {
                header.ProjectAssets.Add(reader.ReadLine());

            }

            header.HasMotionData = int.Parse(reader.ReadLine());
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }

        return header;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        _ = sb.AppendLine(this.LeadInt.ToString()).AppendLine(this.ProjectAssets.Count.ToString()).AppendLine(string.Join("\r\n", this.ProjectAssets));
        _ = this.HasMotionData == 1 ? sb.AppendLine(this.HasMotionData.ToString()) : sb.Append(this.HasMotionData.ToString());
        return sb.ToString();
    }
    public int GetLineCount()
    {
        return 2 + this.ProjectAssets.Count;
    }
}
