using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Pandora.Core.Patchers.Skyrim;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;
public class PackFileGraph : PackFile
{

    private XElement? eventNameContainer;

    private XElement? eventFlagContainer;

    private XElement? variableNameContainer;

    private XElement? variableValueContainer;

    private XElement? variableTypeContainer;

    public uint InitialEventCount { get; private set; } = 0;
    public uint InitialVariableCount { get; private set; } = 0;

    public string EventNamesPath { get; private set; }

    public string EventFlagsPath { get; private set; }

    public string VariableNamesPath { get; private set; }

    public string VariableValuesPath { get; private set; }

    public string VariableTypesPath { get; private set; }

    [MemberNotNull(nameof(EventNamesPath), nameof(EventFlagsPath), nameof(VariableNamesPath), nameof(VariableValuesPath), nameof(VariableTypesPath))]
    private void LoadEventsAndVariables()
    {
        this.TryBuildClassLookup();
        XElement stringDataContainer = this.classLookup["hkbBehaviorGraphStringData"].First();

        XElement variableValueSetContainer = this.classLookup["hkbVariableValueSet"].First();

        XElement graphDataContainer = this.classLookup["hkbBehaviorGraphData"].First();

        string stringDataPath = this.Map.GenerateKey(stringDataContainer);

        string variableValueSetPath = this.Map.GenerateKey(variableValueSetContainer);

        string graphDataPath = this.Map.GenerateKey(graphDataContainer);

        this.MapNode(stringDataPath);
        this.MapNode(variableValueSetPath);
        this.MapNode(graphDataPath);

        this.EventNamesPath = $"{stringDataPath}/eventNames";
        this.EventFlagsPath = $"{graphDataPath}/eventInfos";

        this.VariableNamesPath = $"{stringDataPath}/variableNames";
        this.VariableValuesPath = $"{variableValueSetPath}/wordVariableValues";
        this.VariableTypesPath = $"{graphDataPath}/variableInfos";

        this.eventNameContainer = this.Map.Lookup(this.EventNamesPath);

        this.eventFlagContainer = this.Map.Lookup(this.EventFlagsPath);

        this.variableNameContainer = this.Map.Lookup(this.VariableNamesPath);

        this.variableValueContainer = this.Map.Lookup(this.VariableValuesPath);

        this.variableTypeContainer = this.Map.Lookup(this.VariableTypesPath);

        XAttribute? eventCountAttribute = this.eventNameContainer.Attribute("numelements");
        XAttribute? variableCountAttribute = this.variableNameContainer.Attribute("numelements");
        this.InitialEventCount = eventCountAttribute != null ? uint.Parse(eventCountAttribute.Value) : (uint)this.eventNameContainer.Elements().Count();
        this.InitialVariableCount = variableCountAttribute != null ? uint.Parse(variableCountAttribute.Value) : (uint)this.variableNameContainer.Elements().Count();
    }

    public List<string> GetAnimationFilePaths()
    {
        this.TryBuildClassLookup();

        const string clipGeneratorName = "hkbClipGenerator";
        if (this.classLookup == null || !this.classLookup.Contains(clipGeneratorName)) { return new List<string>(); }

        List<string> animationFilePaths = new();

        IEnumerable<XElement> clipGenerators = this.classLookup[clipGeneratorName];

        foreach (XElement clipGenerator in clipGenerators)
        {
            XElement? animationParam = clipGenerator.Elements().FirstOrDefault(e => e.Attribute("name")?.Value == "animationName");
            if (animationParam == null || animationParam.Value.Length == 0)
            {
                continue;
            }

            animationFilePaths.Add(animationParam.Value);
        }
        return animationFilePaths;
    }

    public PackFileGraph(FileInfo file, Project project) : base(file, project)
    {

    }

    public PackFileGraph(FileInfo file) : base(file)
    {

    }

    public override void Activate()
    {
        if (!this.CanActivate())
        {
            return;
        }

        base.Activate();
        this.LoadEventsAndVariables();
    }
    public List<XElement> EventNames => this.eventNameContainer!.Elements().ToList();

    public List<XElement> EventFlags => this.eventFlagContainer!.Elements().ToList();

    public List<XElement> VariableNames => this.variableNameContainer!.Elements().ToList();

    public List<XElement> VariableValues => this.variableValueContainer!.Elements().ToList();

    public List<XElement> VariableTypes => this.variableTypeContainer!.Elements().ToList();
}
