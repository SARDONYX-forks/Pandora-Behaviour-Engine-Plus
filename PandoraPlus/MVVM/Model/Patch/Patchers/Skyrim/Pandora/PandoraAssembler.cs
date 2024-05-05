using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Pandora.Core;
using Pandora.Core.Patchers.Skyrim;
using Pandora.Patch.Patchers.Skyrim.AnimData;
using Pandora.Patch.Patchers.Skyrim.AnimSetData;
using Pandora.Patch.Patchers.Skyrim.Hkx;
using Pandora.Patch.Patchers.Skyrim.Nemesis;
using ChangeType = Pandora.Patch.Patchers.Skyrim.Hkx.IPackFileChange.ChangeType;

namespace Pandora.Patch.Patchers.Skyrim.Pandora;

public class PandoraAssembler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public ProjectManager ProjectManager { get; private set; }
    public AnimDataManager AnimDataManager { get; private set; }
    public AnimSetDataManager AnimSetDataManager { get; private set; }

    private readonly DirectoryInfo engineFolder = new(Directory.GetCurrentDirectory() + "\\Pandora_Engine");

    private readonly DirectoryInfo templateFolder = new(Directory.GetCurrentDirectory() + "\\Pandora_Engine\\Skyrim\\Template");

    private readonly DirectoryInfo outputFolder = new($"{Directory.GetCurrentDirectory()}\\meshes");

    private readonly Dictionary<string, FileInfo> cachedFiles = new();
    private static readonly Dictionary<string, ChangeType> changeTypeNameMap = Enum.GetValues(typeof(ChangeType)).Cast<ChangeType>().ToDictionary(c => c.ToString(), v => v, StringComparer.OrdinalIgnoreCase);

    public PandoraAssembler()
    {
        this.ProjectManager = new ProjectManager(this.templateFolder, this.outputFolder);
        this.AnimSetDataManager = new AnimSetDataManager(this.templateFolder, this.outputFolder);
        this.AnimDataManager = new AnimDataManager(this.templateFolder, this.outputFolder);
    }
    public PandoraAssembler(NemesisAssembler nemesisAssembler)
    {
        this.ProjectManager = nemesisAssembler.ProjectManager;
        this.AnimSetDataManager = nemesisAssembler.AnimSetDataManager;
        this.AnimDataManager = nemesisAssembler.AnimDataManager;
    }
    public PandoraAssembler(ProjectManager projManager, AnimSetDataManager animSDManager, AnimDataManager animDManager)
    {
        this.ProjectManager = projManager;
        this.AnimSetDataManager = animSDManager;
        this.AnimDataManager = animDManager;
    }
    public void AssembleEdit(ChangeType changeType, XElement element, PackFileChangeSet changeSet)
    {
        XAttribute? pathAttribute = element.Attribute("path");
        if (pathAttribute == null) { return; }

        bool isPathEmpty = string.IsNullOrWhiteSpace(pathAttribute.Value);

        XAttribute? textAttribute = element.Attribute("text");
        XAttribute? preTextAttribute = element.Attribute("preText");

        switch (changeType)
        {
            case ChangeType.Remove:
                if (textAttribute == null)
                {
                    changeSet.AddChange(new RemoveElementChange(pathAttribute.Value));
                    break;
                }
                //assume text
                if (string.IsNullOrWhiteSpace(element.Value) || string.IsNullOrWhiteSpace(textAttribute.Value)) { break; }

                if (preTextAttribute == null)
                {
                    changeSet.AddChange(new RemoveTextChange(pathAttribute.Value, textAttribute.Value));
                    break;
                }
                changeSet.AddChange(new ReplaceTextChange(pathAttribute.Value, preTextAttribute.Value, textAttribute.Value, string.Empty));

                break;

            case ChangeType.Insert:
                if (element.IsEmpty) { break; }
                if (element.HasElements)
                {
                    if (!isPathEmpty)
                    {
                        foreach (XElement childElement in element.Elements()) { changeSet.AddChange(new InsertElementChange(pathAttribute.Value, childElement)); }
                        break;
                    }

                    foreach (XElement childElement in element.Elements()) { changeSet.AddChange(new PushElementChange(PackFile.ROOT_CONTAINER_NAME, element)); }
                    break;
                }
                if (textAttribute == null || isPathEmpty) { break; }

                changeSet.AddChange(new InsertTextChange(pathAttribute.Value, textAttribute.Value, element.Value));

                break;
            case ChangeType.Append:
                if (element.IsEmpty) { break; }
                if (element.HasElements)
                {
                    if (!isPathEmpty)
                    {
                        foreach (XElement childElement in element.Elements()) { changeSet.AddChange(new AppendElementChange(pathAttribute.Value, childElement)); }
                        break;
                    }

                    foreach (XElement childElement in element.Elements()) { changeSet.AddChange(new PushElementChange(PackFile.ROOT_CONTAINER_NAME, element)); }
                    break;
                }

                if (isPathEmpty) { break; }
                changeSet.AddChange(new AppendTextChange(pathAttribute.Value, element.Value));

                break;

            case ChangeType.Replace:
                if (element.IsEmpty || isPathEmpty) { break; }
                if (textAttribute == null && element.HasElements)
                {
                    foreach (XElement childElement in element.Elements()) { changeSet.AddChange(new ReplaceElementChange(pathAttribute.Value, new XElement(childElement))); }
                    break;
                }
                if (textAttribute == null) { break; }
                if (preTextAttribute == null)
                {
                    changeSet.AddChange(new ReplaceTextChange(pathAttribute.Value, string.Empty, textAttribute.Value, element.Value));
                    break;
                }
                changeSet.AddChange(new ReplaceTextChange(pathAttribute.Value, preTextAttribute.Value, textAttribute.Value, element.Value));
                break;

            default:
                break;

        }
    }

    public void AssembleTypedEdits(ChangeType changeType, XElement container, PackFileChangeSet changeSet)
    {
        foreach (XElement element in container.Elements())
        {
            this.AssembleEdit(changeType, element, changeSet);
        }
    }

    public void AssembleEdits(XElement container, PackFileChangeSet changeSet)
    {
        if (!container.HasElements) { return; }
        foreach (XElement element in container.Elements())
        {

            if (changeTypeNameMap.TryGetValue(element.Name.ToString(), out ChangeType changeType))
            {
                if (element.HasAttributes)
                {
                    this.AssembleEdit(changeType, element, changeSet);
                    continue;
                }
                this.AssembleTypedEdits(changeType, element, changeSet);
                continue;
            }
            this.AssembleEdits(element, changeSet);

        }
    }
    public bool AssemblePackFilePatch(FileInfo file, IModInfo modInfo)
    {

        string name = Path.GetFileNameWithoutExtension(file.Name);
        PackFile targetPackFile;
        if (!this.ProjectManager.TryActivatePackFile(name, out targetPackFile!)) { return false; }

        PackFileChangeSet changeSet = new(modInfo);

        XElement container;
        using (FileStream stream = file.OpenRead())
        {
            container = XElement.Load(stream);
        }
        XElement editContainer = container;

        if (editContainer == null) { return false; }

        this.AssembleEdits(editContainer, changeSet);

        targetPackFile.Dispatcher.AddChangeSet(changeSet);
        return true;
    }
    public void AssemblePatch(IModInfo modInfo)
    {
        DirectoryInfo patchFolder = new(Path.Join(modInfo.Folder.FullName, "patches"));
        foreach (FileInfo file in patchFolder.GetFiles("*.xml"))
        {
            _ = this.AssemblePackFilePatch(file, modInfo);
        }
    }
    public void AssembleAnimDataPatch(DirectoryInfo folder)
    {
        FileInfo[] files = folder.GetFiles();
        foreach (FileInfo file in files)
        {
            if (!file.Exists || !this.ProjectManager.TryGetProject(Path.GetFileNameWithoutExtension(file.Name.ToLower()), out Project? targetProject))
            {
                continue;
            }

            using FileStream readStream = file.OpenRead();
            using StreamReader reader = new(readStream);
            string? expectedLine;
            while ((expectedLine = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(expectedLine))
                {
                    continue;
                }

                targetProject!.AnimData?.AddDummyClipData(expectedLine);
            }
        }
    }
    public void AssembleAnimSetDataPatch(DirectoryInfo directoryInfo) //not exactly Nemesis format but this format is just simpler
    {

        foreach (DirectoryInfo subDirInfo in directoryInfo.GetDirectories())
        {
            if (!this.AnimSetDataManager.AnimSetDataMap.TryGetValue(subDirInfo.Name, out ProjectAnimSetData? targetAnimSetData))
            {
                return;
            }

            FileInfo[] patchFiles = subDirInfo.GetFiles();

            foreach (FileInfo patchFile in patchFiles)
            {
                if (!targetAnimSetData.AnimSetsByName.TryGetValue(patchFile.Name, out AnimSet? targetAnimSet))
                {
                    continue;
                }

                using FileStream readStream = patchFile.OpenRead();

                using StreamReader reader = new(readStream);

                string? expectedPath;
                while ((expectedPath = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(expectedPath))
                    {
                        continue;
                    }

                    string animationName = Path.GetFileNameWithoutExtension(expectedPath);
                    string folder = Path.GetDirectoryName(expectedPath)!;
                    SetCachedAnimInfo animInfo = SetCachedAnimInfo.Encode(folder, animationName);
                    targetAnimSet.AddAnimInfo(animInfo);
                }
            }
        }

    }
}
