using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Pandora.Core;
using ChangeType = Pandora.Patch.Patchers.Skyrim.Hkx.IPackFileChange.ChangeType;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;
public class PackFileChangeSet
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<ChangeType, List<IPackFileChange>> changes = new();

    private static readonly IOrderedEnumerable<ChangeType> orderedChangeTypes = Enum.GetValues(typeof(ChangeType)).Cast<ChangeType>().OrderBy(t => t);

    public IModInfo Origin { get; set; }

    public PackFileChangeSet(IModInfo modInfo)
    {
        foreach (ChangeType changeType in orderedChangeTypes) { this.changes.Add(changeType, new List<IPackFileChange>()); }
        this.Origin = modInfo;
    }

    public PackFileChangeSet(PackFileChangeSet packFileChangeSet)
    {
        foreach (ChangeType changeType in orderedChangeTypes) { this.changes.Add(changeType, new List<IPackFileChange>()); }
        this.Origin = packFileChangeSet.Origin;
    }

    public void AddChange(IPackFileChange change)
    {
        this.changes[change.Type].Add(change);
    }

    public static void ApplyInOrder(PackFile packFile, List<PackFileChangeSet> changeSetList)
    {
        foreach (ChangeType changeType in orderedChangeTypes)
        {
            foreach (PackFileChangeSet changeSet in changeSetList)
            {
                changeSet.ApplyForType(packFile, changeType);
            }
        }
    }

    public void ApplyForType(PackFile packFile, ChangeType changeType)
    {
        List<IPackFileChange> changeList = this.changes[changeType];
        foreach (IPackFileChange change in changeList)
        {
            if (!change.Apply(packFile)) { Logger.Warn($"Dispatcher > \"{this.Origin.Name}\" > {packFile.ParentProject?.Identifier}~{packFile.Name} > {change.Type} > {change.AssociatedType} > {change.Path} > FAILED"); }
        }
    }
    public void Apply(PackFile packFile)
    {
        foreach (ChangeType changeType in orderedChangeTypes)
        {
            List<IPackFileChange> changeList = this.changes[changeType];
            foreach (IPackFileChange change in changeList)
            {
                if (!change.Apply(packFile)) { Logger.Warn($"Dispatcher > \"{this.Origin.Name}\" > {packFile.ParentProject?.Identifier}~{packFile.Name} > {change.Type} > {change.AssociatedType} > {change.Path} > FAILED"); }
            }
        }

    }

    public void Validate(PackFile packFile, PackFileValidator validator)
    {
        foreach (ChangeType changeType in orderedChangeTypes)
        {
            validator.Validate(packFile, this.changes[changeType]);
        }
    }
}
