using System.Xml;
using System.Xml.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class RemoveElementChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Remove;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Element;

    public string Path { get; private set; }

    private XElement? element { get; set; }

    public RemoveElementChange(string path)
    {
        this.Path = path;
    }
    public bool Apply(PackFile packFile)
    {
        if (!packFile.Map.PathExists(this.Path))
        {
            return false;
        }

        this.element = PackFileEditor.RemoveElement(packFile, this.Path);
        return !packFile.Map.PathExists(this.Path);

    }

    public bool Revert(PackFile packFile)
    {
        if (this.element == null)
        {
            return false;
        }

        string newPath = PackFileEditor.InsertElement(packFile, this.Path, this.element);
        this.Path = string.IsNullOrEmpty(newPath) ? this.Path : newPath;
        return packFile.Map.PathExists(this.Path);
    }
}
