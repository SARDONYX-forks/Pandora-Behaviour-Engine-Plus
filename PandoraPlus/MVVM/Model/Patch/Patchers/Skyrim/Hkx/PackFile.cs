using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HKX2;
using Pandora.Core.Patchers.Skyrim;
using XmlCake.Linq;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;

public class PackFile : IPatchFile, IEquatable<PackFile>
{
    public XMap Map { get; private set; }

    public static readonly string ROOT_CONTAINER_NAME = "__data__";

    public static readonly string ROOT_CONTAINER_INSERT_PATH = "__data__/top";

    private static readonly HashSet<FileInfo> exportedFiles = new();

    public string Name { get; private set; }
    public FileInfo InputHandle { get; private set; }

    public FileInfo OutputHandle { get; private set; }

    public static bool DebugFiles { get; set; } = false;

    public PackFileEditor Editor { get; private set; } = new PackFileEditor();

    public XElement ContainerNode { get; private set; }

    public PackFileDispatcher Dispatcher { get; private set; } = new PackFileDispatcher();

    public bool ExportSuccess { get; private set; } = true;

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public void ApplyChanges()
    {
        this.Dispatcher.ApplyChanges(this);
    }

    private readonly HashSet<string> mappedNodeNames = new();

    public Project? ParentProject
    {
        get => this.parentProject;
        set
        {
            this.parentProject = value;
            this.UniqueName = $"{this.ParentProject?.Identifier}~{this.Name}";
        }
    }

    protected ILookup<string, XElement>? classLookup = null;

    private bool active = false;
    private Project? parentProject;

    public PackFile(FileInfo file)
    {

        this.InputHandle = file;
        this.OutputHandle = new FileInfo(file.FullName.Replace("Template", "meshes").Replace("\\Pandora_Engine\\Skyrim", ""));
        using (FileStream stream = file.OpenRead())
        {
            this.Map = XMap.Load(stream);
        }

        this.ContainerNode = this.Map.NavigateTo(ROOT_CONTAINER_NAME);
        this.Name = Path.GetFileNameWithoutExtension(this.InputHandle.Name).ToLower();

        this.UniqueName = this.Name;
        //#if DEBUG
        //		Debug.WriteLine($"- {UniqueName}");
        //#endif
    }
    public PackFile(FileInfo file, Project project)
    {
        this.InputHandle = file;
        this.OutputHandle = new FileInfo(file.FullName.Replace("Template", "meshes").Replace("\\Pandora_Engine\\Skyrim", ""));
        using (FileStream stream = file.OpenRead())
        {
            this.Map = XMap.Load(stream);
        }
        this.ParentProject = project;
        this.ContainerNode = this.Map.NavigateTo(ROOT_CONTAINER_NAME);
        this.Name = Path.GetFileNameWithoutExtension(this.InputHandle.Name).ToLower();

        this.UniqueName = $"{this.ParentProject?.Identifier}~{this.Name}";
        //#if DEBUG
        //		Debug.WriteLine(UniqueName);
        //#endif
    }

    [MemberNotNull(nameof(classLookup))]
    public void BuildClassLookup()
    {
        this.classLookup = this.Map.NavigateTo(ROOT_CONTAINER_NAME).Elements().ToLookup(e => e.Attribute("class")!.Value);
    }

    protected bool CanActivate()
    {
        return !this.active;
    }

    public virtual void Activate()
    {
        if (!this.CanActivate())
        {
            return;
        }

        this.Map.MapLayer(ROOT_CONTAINER_NAME, true);
        this.active = true;
    }

    public PackFile(string filePath) : this(new FileInfo(filePath)) { }

    public string UniqueName { get; private set; }
    public XElement SafeNavigateTo(string path)
    {
        return this.Map.NavigateTo(path, this.ContainerNode);
    }

    [MemberNotNull(nameof(classLookup))]
    public void TryBuildClassLookup() { if (this.classLookup == null) { this.BuildClassLookup(); } }
    public XElement GetFirstNodeOfClass(string className)
    {
        this.TryBuildClassLookup();

        return this.classLookup[className].First();
    }

    public void DeleteExistingOutput()
    {
        if (this.OutputHandle.Exists)
        {
            this.OutputHandle.Delete();
        }
#if DEBUG
        FileInfo debugOuputHandle = new(this.OutputHandle.FullName + ".xml");
        if (debugOuputHandle.Exists) { debugOuputHandle.Delete(); }

        debugOuputHandle = new FileInfo(debugOuputHandle.DirectoryName + "\\m_" + debugOuputHandle.Name);
        if (debugOuputHandle.Exists) { debugOuputHandle.Delete(); }
#endif
    }

    public void MapNode(string nodeName)
    {
        lock (this.mappedNodeNames)
        {
            if (this.mappedNodeNames.Contains(nodeName))
            {
                return;
            }

            _ = this.mappedNodeNames.Add(nodeName);
        }
        lock (this.Map)
        {
            this.Map.MapSlice(nodeName);
        }
    }
    public void FlagExportFailure()
    {
        this.ExportSuccess = false;
    }
    public bool Export()
    {

        if (this.OutputHandle.Directory == null)
        {
            return false;
        }

        if (!this.OutputHandle.Directory.Exists) { this.OutputHandle.Directory.Create(); }
        if (this.OutputHandle.Exists) { this.OutputHandle.Delete(); }
        HKXHeader header = HKXHeader.SkyrimSE();
        IHavokObject rootObject;

#if DEBUG
        FileInfo debugOuputHandle = new(this.OutputHandle.DirectoryName + "\\m_" + this.OutputHandle.Name + ".xml");

        using (FileStream writeStream = debugOuputHandle.Create())
        {
            this.Map.Save(writeStream);
        }
        using (MemoryStream memoryStream = new())
        {
            this.Map.Save(memoryStream);
            memoryStream.Position = 0;
            XmlDeserializer deserializer = new();
            rootObject = deserializer.Deserialize(memoryStream, header, false);
        }
        using (FileStream writeStream = this.OutputHandle.Create())
        {
            BinaryWriterEx binaryWriter = new(writeStream);
            PackFileSerializer serializer = new();
            serializer.Serialize(rootObject, binaryWriter, header);
        }

        debugOuputHandle = new FileInfo(this.OutputHandle.FullName + ".xml");

        using (FileStream writeStream = debugOuputHandle.Create())
        {

            XmlSerializer xmlSerializer = new();
            xmlSerializer.Serialize(rootObject, header, writeStream);
        }

#else
		try
		{
		using (var memoryStream = new MemoryStream())
		{
			Map.Save(memoryStream);
			memoryStream.Position = 0;
			var deserializer = new XmlDeserializer();
			rootObject = deserializer.Deserialize(memoryStream, header, false);
		}
		using (var writeStream = OutputHandle.Create())
		{
			var binaryWriter = new BinaryWriterEx(writeStream);
			var serializer = new PackFileSerializer();
			serializer.Serialize(rootObject, binaryWriter, header);
		}
		}
		catch(Exception ex)
		{
			Logger.Fatal($"Export > {ParentProject?.Identifier}~{Name} > FAILED > {ex.ToString()}");
			using (var writeStream = OutputHandle.Create())
			{
				Map.Save(writeStream);
			}
			ExportSuccess = false;
		}
#endif

        return this.ExportSuccess;
    }
    public static void Unpack(FileInfo inputHandle)
    {
        HKXHeader header = HKXHeader.SkyrimSE();
        IHavokObject rootObject;
        using (FileStream readStream = inputHandle.OpenRead())
        {
            XmlDeserializer deserializer = new();
            rootObject = deserializer.Deserialize(readStream, header, false);
        }
        using (FileStream writeStream = inputHandle.Create())
        {
            BinaryWriterEx binaryWriter = new(writeStream);
            PackFileSerializer serializer = new();
            serializer.Serialize(rootObject, binaryWriter, header);
        }
        FileInfo outputHandle = new(Path.ChangeExtension(inputHandle.FullName, ".xml"));
        if (outputHandle.Exists) { outputHandle.Delete(); }
        using (FileStream writeStream = outputHandle.Create())
        {

            XmlSerializer xmlSerializer = new();
            xmlSerializer.Serialize(rootObject, header, writeStream);
        }
    }
    public static FileInfo GetUnpackedHandle(FileInfo inputHandle)
    {
        HKXHeader header = HKXHeader.SkyrimSE();
        IHavokObject rootObject;
        using (FileStream readStream = inputHandle.OpenRead())
        {
            PackFileDeserializer deserializer = new();
            BinaryReaderEx binaryReaderEx = new(readStream);
            rootObject = deserializer.Deserialize(binaryReaderEx);
        }

        FileInfo outputHandle = new(Path.ChangeExtension(inputHandle.FullName, ".xml"));
        if (outputHandle.Exists) { outputHandle.Delete(); }
        using (FileStream writeStream = outputHandle.Create())
        {

            XmlSerializer xmlSerializer = new();
            xmlSerializer.Serialize(rootObject, header, writeStream);
        }

        return outputHandle;
    }

    public bool Equals(PackFile? other)
    {
        return other != null && other.OutputHandle.FullName.Equals(this.OutputHandle.FullName, StringComparison.OrdinalIgnoreCase);
    }
    public override int GetHashCode()
    {
        return this.OutputHandle.FullName.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is not null and PackFile && this.Equals((PackFile)obj);
    }
}
