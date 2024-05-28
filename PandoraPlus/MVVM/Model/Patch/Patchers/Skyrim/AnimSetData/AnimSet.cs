using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimSetData;

public class AnimSet
{
    public string VersionName { get; private set; } = "V3";
    public int NumTriggers { get; private set; } = 0;

    public int NumConditions { get; private set; } = 0;

    public int NumAttackEntries { get; private set; } = 0;

    public int NumAnimationInfos { get; private set; } = 0;

    public List<string> Triggers { get; private set; } = new List<string>();

    public List<SetCondition> Conditions { get; private set; } = new List<SetCondition>();

    public List<SetAttackEntry> AttackEntries { get; private set; } = new List<SetAttackEntry>();

    public List<SetCachedAnimInfo> AnimInfos { get; private set; } = new List<SetCachedAnimInfo>();

    public void AddAnimInfo(SetCachedAnimInfo animInfo)
    {
        this.AnimInfos.Add(animInfo);
    }

    public static AnimSet Read(StreamReader reader)
    {
        AnimSet animSet = new()
        {
            VersionName = reader.ReadLineSafe()
        };

        if (!int.TryParse(reader.ReadLineSafe(), out int numTriggers))
        {
            return animSet;
        }

        for (int i = 0; i < numTriggers; i++) { animSet.Triggers.Add(reader.ReadLineSafe()); }

        if (!int.TryParse(reader.ReadLineSafe(), out int numConditions))
        {
            return animSet;
        }

        for (int i = 0; i < numConditions; i++) { animSet.Conditions.Add(SetCondition.ReadCondition(reader)); }

        if (!int.TryParse(reader.ReadLineSafe(), out int numAttacks))
        {
            return animSet;
        }

        for (int i = 0; i < numAttacks; i++) { animSet.AttackEntries.Add(SetAttackEntry.ReadEntry(reader)); }

        if (!int.TryParse(reader.ReadLineSafe(), out int numAnimationInfos))
        {
            return animSet;
        }

        for (int i = 0; i < numAnimationInfos; i++) { animSet.AnimInfos.Add(SetCachedAnimInfo.Read(reader)); }

        animSet.SyncCounts();

        return animSet;
    }
    private void SyncCounts()
    {
        this.NumTriggers = this.Triggers.Count;
        this.NumConditions = this.Conditions.Count;
        this.NumAttackEntries = this.AttackEntries.Count;
        this.NumAnimationInfos = this.AnimInfos.Count;
    }
    public override string ToString()
    {
        this.SyncCounts();

        StringBuilder sb = new();

        _ = sb.AppendLine(this.VersionName);

        _ = sb.AppendLine(this.NumTriggers.ToString());
        if (this.NumTriggers > 0) { _ = sb.AppendJoin("\r\n", this.Triggers).AppendLine(); }

        _ = sb.AppendLine(this.NumConditions.ToString());
        if (this.NumConditions > 0) { _ = sb.AppendJoin("", this.Conditions); }

        _ = sb.AppendLine(this.NumAttackEntries.ToString());
        if (this.NumAttackEntries > 0) { _ = sb.AppendJoin("\r\n", this.AttackEntries).AppendLine(); }

        _ = sb.AppendLine(this.NumAnimationInfos.ToString());
        if (this.NumAnimationInfos > 0) { _ = sb.AppendJoin("", this.AnimInfos); }

        return sb.ToString();
    }

}
