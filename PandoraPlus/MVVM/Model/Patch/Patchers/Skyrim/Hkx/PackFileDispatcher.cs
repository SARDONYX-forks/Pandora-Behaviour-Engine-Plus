using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class PackFileDispatcher
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    //public ObservableCollection<IPackFileChange> ChangeHistory { get; set; } = new List<IPackFileChange>();

    private List<IPackFileChange> elementChanges { get; set; } = new List<IPackFileChange>();

    private List<IPackFileChange> textChanges { get; set; } = new List<IPackFileChange>();

    private List<PackFileChangeSet> changeSets { get; set; } = new List<PackFileChangeSet>();

    private static readonly Regex EventFormat = new(@"[$]{1}eventID{1}[\[]{1}(.+)[\]]{1}[$]{1}");
    private static readonly Regex VarFormat = new(@"[$]{1}variableID{1}[\[]{1}(.+)[\]]{1}[$]{1}");

    private PackFileValidator packFileValidator { get; set; } = new PackFileValidator();

    public void AddChange(IPackFileChange change)
    {
        if (change.AssociatedType == System.Xml.XmlNodeType.Element)
        {
            this.elementChanges.Add(change);
        }
        else if (change.AssociatedType == System.Xml.XmlNodeType.Text)
        {
            this.textChanges.Add(change);
        }
    }

    public void AddChangeSet(PackFileChangeSet changeSet)
    {
        lock (this.changeSets)
        {
            this.changeSets.Add(changeSet);
        }
    }

    public void SortChangeSets()
    {
        this.changeSets = this.changeSets.OrderBy(s => s.Origin.Priority).ToList();
    }
    public void ApplyChanges(PackFile packFile)
    {
        this.SortChangeSets();

        PackFileChangeSet.ApplyInOrder(packFile, this.changeSets);

        if (packFile is not PackFileGraph) { return; }

        _ = this.packFileValidator.ValidateEventsAndVariables((PackFileGraph)packFile);

        foreach (PackFileChangeSet changeSet in this.changeSets) { changeSet.Validate(packFile, this.packFileValidator); }
    }
}
