using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimSetData;

public class ProjectAnimSetData
{
    public int NumSets { get; private set; } = 1;

    public List<string> AnimSetFileNames { get; private set; } = new List<string>();

    public List<AnimSet> AnimSets { get; private set; } = new List<AnimSet>();

    public Dictionary<string, AnimSet> AnimSetsByName { get; private set; } = new Dictionary<string, AnimSet>();

    public static ProjectAnimSetData Read(StreamReader reader)
    {
        ProjectAnimSetData setData = new();

        if (!int.TryParse(reader.ReadLine(), out int numSets)) { return setData; }
        setData.NumSets = numSets;

        for (int i = 0; i < numSets; i++)
        {
            string fileName = reader.ReadLineSafe();
            setData.AnimSetFileNames.Add(fileName);
        }

        for (int i = 0; i < numSets; i++)
        {
            AnimSet animSet = AnimSet.Read(reader);
            setData.AnimSets.Add(animSet);
            setData.AnimSetsByName.Add(setData.AnimSetFileNames[i], animSet);
        }

        return setData;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        _ = sb.AppendLine(this.NumSets.ToString());
        if (this.NumSets > 0)
        {
            _ = sb.AppendJoin("\r\n", this.AnimSetFileNames).AppendLine();
            _ = sb.AppendJoin("", this.AnimSets);
        }

        return sb.ToString();

    }
}
