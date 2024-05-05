using System.Xml;
using System.Xml.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class ReplaceElementChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Replace;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Element;

    public string Path { get; private set; }

    private XElement element { get; set; }

    public ReplaceElementChange(string path, XElement element)
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

        this.element = PackFileEditor.ReplaceElement(packFile, this.Path, this.element);
        return this.element != null;
    }

    public bool Revert(PackFile packFile)
    {
        return this.Apply(packFile);
    }
}
