using System.Xml.Linq;
using HKX2;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;
public class PatchNodeCreator
{
    private readonly XmlSerializer serializer = new();

    private readonly string newNodePrefix;
    private uint nodeCount = 0;

    public PatchNodeCreator(string newPrefix)
    {
        this.newNodePrefix = newPrefix;
    }
    public string GenerateNodeName(string uniqueName)
    {
        string name = $"#{uniqueName}${this.nodeCount}i";
        this.nodeCount++;
        return name;
    }
    public XElement TranslateToLinq<T>(T hkobject, string nodeName) where T : IHavokObject
    {
        XElement element = this.serializer.WriteDetachedNode(hkobject, nodeName);
        hkobject.WriteXml(this.serializer, element);
        return element;
    }

    public hkbBehaviorReferenceGenerator CreateBehaviorReferenceGenerator(string behaviorName, out string nodeName)
    {
        nodeName = this.GenerateNodeName(behaviorName);
        hkbBehaviorReferenceGenerator behaviorRefNode = new() { m_name = $"{behaviorName}ReferenceGenerator", m_behaviorName = behaviorName, m_variableBindingSet = null, m_userData = 0 };

        return behaviorRefNode;
    }
    public hkbBehaviorReferenceGenerator CreateBehaviorReferenceGenerator(string generatorName, string behaviorName, out string nodeName)
    {
        nodeName = this.GenerateNodeName(generatorName);
        hkbBehaviorReferenceGenerator behaviorRefNode = new() { m_name = $"{generatorName}_RG", m_behaviorName = behaviorName, m_variableBindingSet = null, m_userData = 0 };

        return behaviorRefNode;
    }

    public hkbStateMachineStateInfo CreateSimpleStateInfo(hkbGenerator generator)
    {
        string nodeName = this.GenerateNodeName(generator.m_name);
        hkbStateMachineStateInfo simpleStateInfo = new() { m_name = "PN_SimpleStateInfo", m_probability = 1.0f, m_generator = generator, m_stateId = nodeName.GetHashCode() & 0xfffffff, m_enable = true };

        return simpleStateInfo;
    }

    public hkbStateMachineStateInfo CreateSimpleStateInfo(hkbGenerator generator, out string nodeName)
    {
        nodeName = this.GenerateNodeName(generator.m_name);
        hkbStateMachineStateInfo simpleStateInfo = new() { m_name = "PN_SimpleStateInfo", m_probability = 1.0f, m_generator = generator, m_stateId = nodeName.GetHashCode() & 0xfffffff, m_enable = true };//not working for some reason; out of range? state id collision?

        return simpleStateInfo;
    }
}
