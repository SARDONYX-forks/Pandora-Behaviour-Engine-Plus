using System;
using System.IO;
using HKX2;
using NLog;
using Pandora.Patch.Patchers.Skyrim.Hkx;

namespace Pandora.Patch.IOManagers.Skyrim;
public class PackFileExporter : Exporter<PackFile>
{
    public DirectoryInfo ExportDirectory { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public PackFileExporter()
    {
        this.ExportDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
    }

    public bool Export(PackFile packFile)
    {
        FileInfo outputHandle = new(Path.Join(this.ExportDirectory.FullName, Path.GetRelativePath(Directory.GetCurrentDirectory(), packFile.InputHandle.FullName.Replace("Pandora_Engine\\Skyrim\\Template", "meshes", StringComparison.OrdinalIgnoreCase))));

        if (outputHandle.Directory == null)
        {
            return false;
        }

        if (!outputHandle.Directory.Exists) { outputHandle.Directory.Create(); }
        if (outputHandle.Exists) { outputHandle.Delete(); }
        HKXHeader header = HKXHeader.SkyrimSE();
        IHavokObject rootObject;

        try
        {
            using (MemoryStream memoryStream = new())
            {
                packFile.Map.Save(memoryStream);
                memoryStream.Position = 0;
                XmlDeserializer deserializer = new();
                rootObject = deserializer.Deserialize(memoryStream, header, false);
            }
            using FileStream writeStream = outputHandle.Create();
            BinaryWriterEx binaryWriter = new(writeStream);
            PackFileSerializer serializer = new();
            serializer.Serialize(rootObject, binaryWriter, header);
        }
        catch (Exception ex)
        {
            Logger.Fatal($"Export > {packFile.ParentProject?.Identifier}~{packFile.Name} > FAILED > {ex}");
            using FileStream writeStream = outputHandle.Create();
            packFile.Map.Save(writeStream);
            return false;
        }
        return true;
    }

    public PackFile Import(FileInfo file)
    {
        throw new NotImplementedException();
    }
}
