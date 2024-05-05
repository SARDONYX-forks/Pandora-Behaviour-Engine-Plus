using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Pandora.Core.Patchers.Skyrim;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;
public class PackFileCharacter : PackFile
{
    public PackFileCharacter(FileInfo file) : base(file) { this.LoadAnimationNames(); }

    public PackFileCharacter(FileInfo file, Project project) : base(file, project) { this.LoadAnimationNames(); }

    private XElement? animationNamesContainer;

    public uint InitialAnimationCount { get; private set; } = 0;

    public string AnimationNamesPath { get; private set; }

    public string RigNamePath { get; private set; }

    public string BehaviorFilenamePath { get; private set; }

    [MemberNotNull(nameof(AnimationNamesPath), nameof(RigNamePath), nameof(BehaviorFilenamePath))]
    private void LoadAnimationNames()
    {
        this.TryBuildClassLookup();

        XElement stringDataContainer = this.classLookup["hkbCharacterStringData"].First();

        string characterStringDataPath = this.Map.GenerateKey(stringDataContainer);
        this.Activate();
        this.MapNode(characterStringDataPath);

        this.AnimationNamesPath = $"{characterStringDataPath}/animationNames";
        this.RigNamePath = $"{characterStringDataPath}/rigName";
        this.BehaviorFilenamePath = $"{characterStringDataPath}/behaviorFilename";

        this.animationNamesContainer = this.Map.Lookup(this.AnimationNamesPath);

        XAttribute? animationCountAttribute = this.animationNamesContainer.Attribute("numelements");
        this.InitialAnimationCount = (animationCountAttribute != null) ? uint.Parse(animationCountAttribute.Value) : this.NewAnimationCount;
    }

    public List<XElement> AnimationNames => this.animationNamesContainer!.Elements().ToList();

    public uint NewAnimationCount => (uint)this.AnimationNames.Count - this.InitialAnimationCount;

}
