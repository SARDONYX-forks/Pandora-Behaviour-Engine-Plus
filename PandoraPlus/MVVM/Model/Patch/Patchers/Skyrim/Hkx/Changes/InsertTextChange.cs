using System.Xml;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class InsertTextChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Insert;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Text;

    public string Path { get; private set; }

    private readonly string markerValue;
    private readonly string value;

    public InsertTextChange(string path, string markerValue, string value)
    {
        this.Path = path;
        this.markerValue = markerValue;
        this.value = value;
    }

    public bool Apply(PackFile packFile)
    {
        return PackFileEditor.InsertText(packFile, this.Path, this.markerValue, this.value);
    }

    public bool Revert(PackFile packFile)
    {
        PackFileEditor.RemoveText(packFile, this.Path, this.value);
        return true;
    }
}

public class AppendTextChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Insert;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Text;

    public string Path { get; private set; }

    private string value { get; set; }

    public AppendTextChange(string path, string value)
    {
        this.Path = path;
        this.value = value;
    }

    public bool Apply(PackFile packFile)
    {
        PackFileEditor.AppendText(packFile, this.Path, this.value);
        return true;
    }

    public bool Revert(PackFile packFile)
    {
        PackFileEditor.RemoveText(packFile, this.Path, this.value);
        return true;
    }
}
