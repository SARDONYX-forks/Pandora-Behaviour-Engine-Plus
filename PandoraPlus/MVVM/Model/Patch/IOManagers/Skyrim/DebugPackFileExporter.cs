using System;
using System.IO;
using HKX2;
using Pandora.Patch.Patchers.Skyrim.Hkx;

namespace Pandora.Patch.IOManagers.Skyrim;

public class DebugPackFileExporter : Exporter<PackFile>
{
    public DirectoryInfo ExportDirectory { get; set; }
    public DebugPackFileExporter()
    {
        this.ExportDirectory = new DirectoryInfo(Path.Join(Directory.GetCurrentDirectory()));
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
        FileInfo debugOuputHandle = new(outputHandle.DirectoryName + "\\pre_" + outputHandle.Name + ".xml");

        using (FileStream writeStream = debugOuputHandle.Create())
        {
            packFile.Map.Save(writeStream);
        }
        using (MemoryStream memoryStream = new())
        {
            packFile.Map.Save(memoryStream);
            memoryStream.Position = 0;
            XmlDeserializer deserializer = new();
            rootObject = deserializer.Deserialize(memoryStream, header, false);
        }
        using (FileStream writeStream = outputHandle.Create())
        {
            BinaryWriterEx binaryWriter = new(writeStream);
            PackFileSerializer serializer = new();
            serializer.Serialize(rootObject, binaryWriter, header);
        }
        debugOuputHandle = new FileInfo(outputHandle.FullName + ".xml");

        using (FileStream writeStream = debugOuputHandle.Create())
        {

            XmlSerializer xmlSerializer = new();
            xmlSerializer.Serialize(rootObject, header, writeStream);
        }

        return true;
    }

    public PackFile Import(FileInfo file)
    {
        throw new NotImplementedException();
    }
}
