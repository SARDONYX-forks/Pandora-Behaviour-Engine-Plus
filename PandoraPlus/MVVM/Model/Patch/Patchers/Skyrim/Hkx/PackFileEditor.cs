using System.Collections.Generic;
using System.Xml.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public partial class PackFileEditor
{
    //these tuples are gross, pretend this implementation doesn't exist
    //deprecated

    private List<(string path, XElement element)> ReplaceEdits { get; set; } = new List<(string path, XElement element)>();

    private List<(string path, XElement element)> InsertEdits { get; set; } = new List<(string path, XElement element)>();

    private List<string> RemoveEdits { get; set; } = new List<string>();

    private List<(string path, string oldValue, string newValue)> TextReplaceEdits { get; set; } = new List<(string path, string oldValue, string newValue)>();

    private List<(string path, string insertValue)> TextInsertEdits { get; set; } = new List<(string path, string insertValue)>();

    private List<(string path, string removeValue)> TextRemoveEdits { get; set; } = new List<(string path, string removeValue)>();

    private List<XElement> TopLevelInserts { get; set; } = new List<XElement>();

    public void QueueReplaceElement(string path, XElement element)
    {
        this.ReplaceEdits.Add((path, element));
    }

    public void QueueInsertElement(string path, XElement element)
    {
        this.InsertEdits.Add((path, element));
    }

    public void QueueRemoveElement(string path)
    {
        this.RemoveEdits.Add(path);
    }

    public void QueueReplaceText(string path, string oldValue, string newValue)
    {
        this.TextReplaceEdits.Add((path, oldValue, newValue));
    }

    public void QueueInsertText(string path, string insertvalue)
    {
        this.TextInsertEdits.Add((path, insertvalue));
    }

    public void QueueRemoveText(string path, string removeValue)
    {
        this.TextRemoveEdits.Add((path, removeValue));
    }

    public void QueueTopLevelInsert(XElement element)
    {
        this.TopLevelInserts.Add(element);
    }

    private void ApplyReplaceEdits(PackFile packFile)
    {
        foreach ((string path, XElement element) in this.ReplaceEdits)
        {
            _ = ReplaceElement(packFile, path, element);
        }
    }

    private void ApplyInsertEdits(PackFile packFile)
    {
        foreach ((string path, XElement element) in this.InsertEdits)
        {
            _ = InsertElement(packFile, path, element);
        }
    }

    private void ApplyRemoveEdits(PackFile packFile)
    {
        foreach (string edit in this.RemoveEdits)
        {
            _ = RemoveElement(packFile, edit);
        }
    }

    private void ApplyTextRemoveEdits(PackFile packFile)
    {
        foreach ((string path, string removeValue) in this.TextRemoveEdits)
        {
            RemoveText(packFile, path, removeValue);
        }
    }

    public void ApplyEdits(PackFile packFile)
    {
        this.ApplyRemoveEdits(packFile);

        this.ApplyReplaceEdits(packFile);

        this.ApplyInsertEdits(packFile);

        this.ApplyTextRemoveEdits(packFile);

    }

}
