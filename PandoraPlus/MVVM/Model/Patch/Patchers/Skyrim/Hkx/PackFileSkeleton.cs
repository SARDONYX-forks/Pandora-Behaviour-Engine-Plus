using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Pandora.Core.Patchers.Skyrim;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;
public class PackFileSkeleton : PackFile
{
    public PackFileSkeleton(FileInfo file) : base(file) { this.LoadSkeletonData(); }

    public PackFileSkeleton(FileInfo file, Project project) : base(file, project) { this.LoadSkeletonData(); }

    public string SkeletonPath { get; private set; }
    public string BonesPath { get; private set; }

    [MemberNotNull(nameof(BonesPath), nameof(SkeletonPath))]
    private void LoadSkeletonData()
    {
        this.TryBuildClassLookup();

        XElement skeletonContainer = this.classLookup["hkaSkeleton"].First();
        this.SkeletonPath = this.Map.GenerateKey(skeletonContainer);
        this.Activate();
        this.MapNode(this.SkeletonPath);

        this.BonesPath = $"{this.SkeletonPath}/bones";

        this.InitialBoneCount = this.BoneCount;
    }

    public uint InitialBoneCount { get; private set; } = 0;
    public uint BoneCount => (uint)this.Map.Lookup(this.BonesPath).Elements().Count();
}
