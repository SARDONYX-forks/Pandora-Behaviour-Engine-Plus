using System.Xml;
using System.Xml.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class AppendElementChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Append;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Element;

    public string Path { get; private set; }

    private XElement element { get; set; }

    public AppendElementChange(string path, XElement element)
    {
        this.Path = path;
        this.element = element;
    }
    public bool Apply(PackFile packFile)
    {

        if (!packFile.Map.PathExists(this.Path))
        {
            return false;
        }

        string newPath = PackFileEditor.AppendElement(packFile, this.Path, this.element);
        this.Path = string.IsNullOrEmpty(newPath) ? this.Path : newPath;
        return packFile.Map.PathExists(this.Path);
    }

    public bool Revert(PackFile packFile)
    {
        if (!packFile.Map.PathExists(this.Path))
        {
            return false;
        }

        _ = PackFileEditor.RemoveElement(packFile, this.Path);
        return true;
    }
}
