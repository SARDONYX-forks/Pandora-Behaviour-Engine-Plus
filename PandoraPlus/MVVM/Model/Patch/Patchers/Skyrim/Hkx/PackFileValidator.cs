using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class PackFileValidator
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly Regex EventFormat = new(@"[$]{1}eventID{1}[\[]{1}(.+)[\]]{1}[$]{1}", RegexOptions.Compiled);
    private static readonly Regex VarFormat = new(@"[$]{1}variableID{1}[\[]{1}(.+)[\]]{1}[$]{1}", RegexOptions.Compiled);

    private readonly Dictionary<string, int> EventIndices = new();
    private readonly Dictionary<string, int> variableIndices = new();

    private static int GetIndexFromMatch(Dictionary<string, int> map, Match match)
    {
        return !match.Success ? -1 : !map.TryGetValue(match.Groups[1].Value, out int index) ? -1 : index;
    }
    public bool ValidateEventsAndVariables(PackFileGraph graph)
    {
        List<XElement> eventNameElements = graph.EventNames;
        List<XElement> eventFlagElements = graph.EventFlags;

        List<XElement> variableNameElements = graph.VariableNames;
        List<XElement> variableValueElements = graph.VariableValues;
        List<XElement> variableTypeElements = graph.VariableTypes;

        eventNameElements.Reverse();
        eventFlagElements.Reverse();

        variableNameElements.Reverse();
        variableValueElements.Reverse();
        variableTypeElements.Reverse();
        //reverse is necessary so that validator doesn't remove the original element if a duplicate is found

        HashSet<string> uniqueEventNames = new();
        HashSet<string> uniqueVariableNames = new(StringComparer.OrdinalIgnoreCase);

        this.EventIndices.Clear();
        this.variableIndices.Clear();

        int eventLowerBound = eventNameElements.Count - (int)graph.InitialEventCount - 1;
        int variableLowerBound = variableNameElements.Count - (int)graph.InitialVariableCount - 1;

        for (int i = eventLowerBound; i >= 0; i--)
        {
            XElement eventNameElement = eventNameElements[i];
            string eventName = eventNameElement.Value.Trim();
            if (!uniqueEventNames.Add(eventName))
            {
                eventNameElement.Remove();
                eventFlagElements[i].Remove();

                eventNameElements.RemoveAt(i);
                eventFlagElements.RemoveAt(i);
                Logger.Warn($"Validator > {graph.ParentProject?.Identifier}~{graph.Name} > Duplicate Event > {eventName} > Index > {i} > REMOVED");
                continue;
            }
            //#if DEBUG
            //				Logger.Debug($"Validator > {graph.ParentProject?.Identifier}~{graph.Name} > Mapped Event > {eventName} > Index {i}");
            //#endif

        }

        for (int i = variableLowerBound; i >= 0; i--)
        {
            XElement variableNameElement = variableNameElements[i];
            string variableName = variableNameElement.Value.Trim();
            if (!uniqueVariableNames.Add(variableName))
            {
                variableNameElement.Remove();
                variableTypeElements[i].Remove();
                variableValueElements[i].Remove();

                variableNameElements.RemoveAt(i);
                variableTypeElements.RemoveAt(i);
                variableValueElements.RemoveAt(i);
                Logger.Warn($"Validator > {graph.ParentProject?.Identifier}~{graph.Name} > Duplicate Variable > {variableName} > Index > {i} > REMOVED");
                continue;
            }
            //#if DEBUG
            //				Logger.Debug($"Validator > {packFile.ParentProject?.Identifier}~{packFile.Name} > Mapped Variable > {variableName} > Index {i}");
            //#endif

        }

        for (int i = 0; i < eventNameElements.Count; i++)
        {
            if (!this.EventIndices.ContainsKey(eventNameElements[i].Value))
            {
                this.EventIndices.Add(eventNameElements[i].Value, eventNameElements.Count - 1 - i);
            }
        }
        for (int i = 0; i < variableNameElements.Count; i++)
        {
            if (!this.variableIndices.ContainsKey(variableNameElements[i].Value))
            {
                this.variableIndices.Add(variableNameElements[i].Value, variableNameElements.Count - 1 - i);
            }
        }
        return true;
    }
    private void ValidateElementText(XElement element, Dictionary<string, int> eventIndices, Dictionary<string, int> variableIndices)
    {
        string rawValue = element.Value;

        MatchCollection eventMatch = EventFormat.Matches(rawValue);
        foreach (Match match in eventMatch.Cast<Match>())
        {
            int index = GetIndexFromMatch(eventIndices, match);

            rawValue = rawValue.Replace(match.Value, index.ToString());
        }

        MatchCollection varMatch = VarFormat.Matches(element.Value);
        foreach (Match match in varMatch.Cast<Match>())
        {
            int index = GetIndexFromMatch(variableIndices, match);
            rawValue = rawValue.Replace(match.Value, index.ToString());
        }
        element.SetValue(rawValue);
    }

    private void ValidateElementContent(XElement element, Dictionary<string, int> eventIndices, Dictionary<string, int> variableIndices)
    {

        if (!element.HasElements)
        {
            this.ValidateElementText(element, eventIndices, variableIndices);
            return;
        }

        foreach (XElement xelement in element.Elements())
        {

            this.ValidateElementContent(xelement, eventIndices, variableIndices);
        }

    }

    public static void TryValidateClipGenerator(string path, PackFile packFile)
    {
        if (!packFile.Map.TryLookup($"{path}/animationName", out _))
        {
            return;
        }

        string clipName = packFile.Map.Lookup($"{path}/name").Value!;
        packFile.ParentProject?.AnimData?.AddDummyClipData(clipName);
    }

    public void Validate(PackFile packFile, params List<IPackFileChange>[] changeLists)
    {
        //if (!ValidateEventsAndVariables(packFile)) return;

        foreach (List<IPackFileChange> changeList in changeLists)
        {
            foreach (IPackFileChange change in changeList)
            {
                if (!packFile.Map.TryLookup(change.Path, out XElement element))
                {
                    continue;
                }
                //ValidateElementCount(element.Parent!); might not be needed with hkx2 library; testing needed.
                this.ValidateElementContent(element, this.EventIndices, this.variableIndices);

            }
        }
    }
}
