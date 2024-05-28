﻿using System.Xml;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class RemoveTextChange : IPackFileChange
{
    public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Remove;

    public XmlNodeType AssociatedType { get; } = XmlNodeType.Text;

    public string Path { get; private set; }
    private string value { get; set; }

    public RemoveTextChange(string path, string value)
    {
        this.Path = path;
        this.value = value;

    }

    public bool Apply(PackFile packFile)
    {
        PackFileEditor.RemoveText(packFile, this.Path, this.value);
        return true;
    }

    public bool Revert(PackFile packFile)
    {
        //PackFileEditor.InsertText(packFile, Path, value);
        return true;
    }
}
