using Pandora.Models.Patch.Skyrim64.Hkx.Changes;
using Pandora.Models.Patch.Skyrim64.Hkx.Packfile;
using System.Xml;

namespace Pandora.Models.Patch.Skyrim64.Hkx.Changes;

public class InsertTextChange : IPackFileChange
{
	public IPackFileChange.ChangeType Type { get; } = IPackFileChange.ChangeType.Insert;

	public XmlNodeType AssociatedType { get; } = XmlNodeType.Text;
	public string Target { get; }
	public string Path { get; private set; }
	private string markerValue;
	private string value;

	public InsertTextChange(string target, string path, string markerValue, string value)
	{
		Target = target;
		Path = path;
		this.markerValue = markerValue;
		this.value = value;
	}
	public bool Apply(PackFile packFile)
	{
		if (!packFile.TryGetXMap(Target, out var xmap))
		{
			return false;
		}
		return PackFileEditor.InsertText(xmap!, Path, markerValue, value);
	}
}