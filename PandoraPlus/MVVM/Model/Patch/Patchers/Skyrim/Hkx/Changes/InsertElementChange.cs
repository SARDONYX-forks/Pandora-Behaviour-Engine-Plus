using System.Xml;
using System.Xml.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class InsertElementChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Insert;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Element;

    public string Path { get; private set; }

    private XElement element { get; set; }

    public InsertElementChange(string path, XElement element)
    {
        this.Path = path;
        this.element = element;
    }
    public bool Apply(PackFile packFile)
    {
        string newPath = PackFileEditor.InsertElement(packFile, this.Path, this.element);
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
