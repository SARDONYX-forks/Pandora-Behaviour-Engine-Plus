using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimSetData;

public class SetAttackEntry
{
    public string AttackTrigger { get; private set; } = "attackStart";

    public int Unk { get; private set; } = 0;

    public int NumClips { get; private set; } = 1;

    public List<string> ClipNames { get; private set; } = new List<string>() { "attackClip" };

    public static SetAttackEntry ReadEntry(StreamReader reader)
    {
        SetAttackEntry entry = new()
        {
            AttackTrigger = reader.ReadLineSafe()
        };

        if (!int.TryParse(reader.ReadLineSafe(), out int unk) || !int.TryParse(reader.ReadLineSafe(), out int numClips))
        {
            return entry;
        }

        entry.NumClips = numClips;
        entry.Unk = unk;

        if (numClips > 0) { entry.ClipNames = new List<string>(); }
        for (int i = 0; i < numClips; i++)
        {
            entry.ClipNames.Add(reader.ReadLineSafe());
        }

        return entry;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        _ = sb.AppendLine(this.AttackTrigger);

        _ = sb.AppendLine(this.Unk.ToString());

        if (this.NumClips > 0)
        {
            _ = sb.AppendLine(this.NumClips.ToString());
            _ = sb.AppendJoin("\r\n", this.ClipNames);
        }
        else
        {
            _ = sb.Append(this.NumClips.ToString());
        }

        return sb.ToString();
    }

}
