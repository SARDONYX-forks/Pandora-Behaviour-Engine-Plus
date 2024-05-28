using System.Xml;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class ReplaceTextChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Replace;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Text;

    public string Path { get; private set; }
    private string oldValue { get; set; }

    private string newValue { get; set; }

    private readonly string preValue;

    public ReplaceTextChange(string path, string preValue, string oldvalue, string newvalue)
    {
        this.Path = path;
        this.oldValue = oldvalue;
        this.newValue = newvalue;
        this.preValue = preValue;
    }
    public bool Apply(PackFile packFile)
    {
        return PackFileEditor.ReplaceText(packFile, this.Path, this.preValue, this.oldValue, this.newValue);

    }

    public bool Revert(PackFile packFile)
    {
        //PackFileEditor.ReplaceText(packFile, Path, newValue, oldValue);
        return true;
    }

}
