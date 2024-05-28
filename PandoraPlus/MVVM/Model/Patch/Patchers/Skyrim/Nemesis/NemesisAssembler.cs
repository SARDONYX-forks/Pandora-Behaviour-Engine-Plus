using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NLog;
using Pandora.Core;
using Pandora.Core.Patchers.Skyrim;
using Pandora.Patch.IOManagers;
using Pandora.Patch.IOManagers.Skyrim;
using Pandora.Patch.Patchers.Skyrim.AnimData;
using Pandora.Patch.Patchers.Skyrim.AnimSetData;
using Pandora.Patch.Patchers.Skyrim.Hkx;
using Pandora.Patch.Patchers.Skyrim.Pandora;
using XmlCake.Linq;
using XmlCake.Linq.Expressions;

namespace Pandora.Patch.Patchers.Skyrim.Nemesis;

public class NemesisAssembler : IAssembler //animdata and animsetdata deviate from nemesis format
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger(); //to do: move logger into inheritable base class

    private readonly IXExpression replacePattern = new XSkipWrapExpression(new XStep(XmlNodeType.Comment, "CLOSE"), new XStep(XmlNodeType.Comment, "OPEN"), new XStep(XmlNodeType.Comment, "ORIGINAL"), new XStep(XmlNodeType.Comment, "CLOSE"));
    private readonly IXExpression insertPattern = new XSkipWrapExpression(new XStep(XmlNodeType.Comment, "ORIGINAL"), new XStep(XmlNodeType.Comment, "OPEN"), new XStep(XmlNodeType.Comment, "CLOSE"));

    //private XPathLookup lookup = new XPathLookup();

    private readonly List<PackFile> packFiles = new();

    private static readonly DirectoryInfo engineFolder = new(Directory.GetCurrentDirectory() + "\\Pandora_Engine");

    private static readonly DirectoryInfo templateFolder = new(Directory.GetCurrentDirectory() + "\\Pandora_Engine\\Skyrim\\Template");

    private static readonly DirectoryInfo outputFolder = new($"{Directory.GetCurrentDirectory()}\\meshes");
    public ProjectManager ProjectManager { get; private set; }
    public AnimDataManager AnimDataManager { get; private set; }
    public AnimSetDataManager AnimSetDataManager { get; private set; }

    private readonly PandoraConverter pandoraConverter;

    private readonly Exporter<PackFile> exporter = new PackFileExporter();
    public NemesisAssembler()
    {

        this.ProjectManager = new ProjectManager(templateFolder, outputFolder);
        this.AnimSetDataManager = new AnimSetDataManager(templateFolder, outputFolder);
        this.AnimDataManager = new AnimDataManager(templateFolder, outputFolder);

        this.pandoraConverter = new PandoraConverter(this.ProjectManager, this.AnimSetDataManager, this.AnimDataManager);
    }
    public NemesisAssembler(Exporter<PackFile> ioManager)
    {
        this.exporter = ioManager;
        this.ProjectManager = new ProjectManager(templateFolder, outputFolder);
        this.AnimSetDataManager = new AnimSetDataManager(templateFolder, outputFolder);
        this.AnimDataManager = new AnimDataManager(templateFolder, outputFolder);

        this.pandoraConverter = new PandoraConverter(this.ProjectManager, this.AnimSetDataManager, this.AnimDataManager);
    }
    public NemesisAssembler(Exporter<PackFile> ioManager, ProjectManager projManager, AnimSetDataManager animSDManager, AnimDataManager animDManager)
    {
        this.exporter = ioManager;
        this.ProjectManager = projManager;
        this.AnimSetDataManager = animSDManager;
        this.AnimDataManager = animDManager;

        this.pandoraConverter = new PandoraConverter(this.ProjectManager, this.AnimSetDataManager, this.AnimDataManager);
    }

    public void LoadResources()
    {
        throw new NotImplementedException();

    }
    public void GetPostMessages(StringBuilder builder)
    {
        this.ProjectManager.GetFNISInfo(builder);
        this.ProjectManager.GetAnimationInfo(builder);
        this.ProjectManager.GetExportInfo(builder);
    }
    public async Task LoadResourcesAsync()
    {
        Task animSetDataTask = Task.Run(this.AnimSetDataManager.SplitAnimSetDataSingleFile);
        await Task.Run(this.ProjectManager.LoadTrackedProjects);
        await Task.Run(() => { this.AnimDataManager.SplitAnimationDataSingleFile(this.ProjectManager); });
        await animSetDataTask;

    }

    public void AssemblePatch(IModInfo modInfo)
    {
        DirectoryInfo folder = modInfo.Folder;
        DirectoryInfo[] subFolders = folder.GetDirectories();

        foreach (DirectoryInfo subFolder in subFolders)
        {
            if (this.AssemblePackFilePatch(subFolder, modInfo))
            {
                continue;
            }

            if (subFolder.Name == "animationsetdatasinglefile")
            {
                this.AssembleAnimSetDataPatch(subFolder);
            }

            if (subFolder.Name == "animationdatasinglefile")
            {
                this.AssembleAnimDataPatch(subFolder);
            }

            if (subFolder.Name == "animdata")
            {
                subFolder.Delete(true);
            }
        }
    }
    public void AssemblePatch(IModInfo modInfo, DirectoryInfo folder)
    {
        DirectoryInfo[] subFolders = folder.GetDirectories();
        foreach (DirectoryInfo subFolder in subFolders)
        {
            if (this.AssemblePackFilePatch(subFolder, modInfo))
            {
                continue;
            }

            if (subFolder.Name == "animationsetdatasinglefile")
            {
                this.AssembleAnimSetDataPatch(subFolder);
            }

            if (subFolder.Name == "animationdatasinglefile")
            {
                this.AssembleAnimDataPatch(subFolder);
            }

            if (subFolder.Name == "animdata")
            {
                subFolder.Delete(true);
            }
        }
    }

    public bool ApplyPatches()
    {
        return this.ProjectManager.ApplyPatches();
    }

    public async Task<bool> ApplyPatchesAsync()
    {

        Task animSetDataTask = Task.Run(this.AnimSetDataManager.MergeAnimSetDataSingleFile);

        Task animDataTask = Task.Run(this.AnimDataManager.MergeAnimDataSingleFile);

        bool mainTask = await this.ProjectManager.ApplyPatchesParallel();
        bool exportSuccess = this.exporter.ExportParallel(this.ProjectManager.ActivePackFiles);

        await animDataTask;
        await animSetDataTask;

        return mainTask && exportSuccess;

    }

    //to-fix: certain excerpts being misclassified as single replace edit when it is actually a replace and insert edit

    //
    public static void ForwardReplaceEdit(PackFile packFile, XMatch match, PackFileChangeSet changeSet, XPathLookup lookup)
    {
        List<XNode> newNodes = new();
        int separatorIndex = match.Count;
        _ = match[0].PreviousNode;

        //foreach(var node in match) { node.Remove();  }
        for (int i = 1; i < separatorIndex; i++)
        {

            XNode node = match[i];

            if (node.NodeType == XmlNodeType.Comment)
            {
                separatorIndex = i;
                break;
            }
            newNodes.Add(node);
        }

        if (newNodes.Count > 0)
        {
            for (int i = separatorIndex + 1; i < match.Count - 1; i++)
            {
                XNode node = match[i];
                XNode newNode = newNodes[i - separatorIndex - 1];
                switch (node.NodeType)
                {
                    case XmlNodeType.Text:

                        StringBuilder previousTextBuilder = new();
                        StringBuilder bufferTextBuilder = new();
                        bool skipText = false;
                        XNode? previousNode = newNode.PreviousNode?.PreviousNode;
                        while (previousNode != null)
                        {
                            if (previousNode.NodeType == XmlNodeType.Comment)
                            {
                                XComment comment = (XComment)previousNode;
                                if (comment.Value.Contains("close", StringComparison.OrdinalIgnoreCase))
                                {
                                    skipText = true;
                                }
                                else if (comment.Value.Contains("open", StringComparison.OrdinalIgnoreCase))
                                {
                                    skipText = false;
                                    _ = previousTextBuilder.Insert(0, bufferTextBuilder);
                                    bufferTextBuilder = bufferTextBuilder.Clear();
                                }
                                else if (comment.Value.Contains("original", StringComparison.OrdinalIgnoreCase))
                                {
                                    skipText = false;
                                    bufferTextBuilder = bufferTextBuilder.Clear();
                                }
                                previousNode = previousNode.PreviousNode;
                                continue;
                            }
                            if (skipText)
                            {
                                _ = bufferTextBuilder.Insert(0, '\n');
                                _ = bufferTextBuilder.Insert(0, previousNode.ToString());
                                previousNode = previousNode.PreviousNode;
                                continue;
                            }
                            _ = previousTextBuilder.Insert(0, '\n');
                            _ = previousTextBuilder.Insert(0, previousNode.ToString());
                            previousNode = previousNode.PreviousNode;
                        }

                        string preText = previousTextBuilder.ToString();
                        string oldText = ((XText)node).Value;
                        string newText = ((XText)newNode).Value;
                        //packFile.Editor.QueueReplaceText(lookup.LookupPath(node), ((XText)node).Value, ((XText)newNodes[i - separatorIndex - 1]).Value);

                        changeSet.AddChange(new ReplaceTextChange(lookup.LookupPath(node), preText, oldText, newText));
                        //lock (packFile.edits) packFile.edits.AddChange(new ReplaceTextChange(lookup.LookupPath(node), ((XText)node).Value, ((XText)newNodes[i - separatorIndex - 1]).Value,modInfo));
                        break;

                    case XmlNodeType.Element:
                        //packFile.Editor.QueueReplaceElement(lookup.LookupPath(node), (XElement)newNodes[i - separatorIndex - 1]);
                        changeSet.AddChange(new ReplaceElementChange(lookup.LookupPath(newNode), (XElement)newNode));
                        //lock (packFile.edits) packFile.edits.AddChange(new ReplaceElementChange(lookup.LookupPath(node), (XElement)newNodes[i - separatorIndex - 1],modInfo));
                        break;
                    case XmlNodeType.None:
                        break;
                    case XmlNodeType.Attribute:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.Entity:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.DocumentFragment:
                        break;
                    case XmlNodeType.Notation:
                        break;
                    case XmlNodeType.Whitespace:
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    default:
                        break;
                }
            }
            return;
        }
        for (int i = separatorIndex + 1; i < match.Count - 1; i++)
        {
            XNode node = match[i];
            switch (node.NodeType)
            {
                case XmlNodeType.Text:
                    break;
                case XmlNodeType.Element:
                    //packFile.Editor.QueueRemoveElement(lookup.LookupPath(node));
                    changeSet.AddChange(new RemoveElementChange(lookup.LookupPath(node)));
                    //lock (packFile.edits) packFile.edits.AddChange(new RemoveElementChange(lookup.LookupPath(node),modInfo));
                    break;
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Attribute:
                    break;
                case XmlNodeType.CDATA:
                    break;
                case XmlNodeType.EntityReference:
                    break;
                case XmlNodeType.Entity:
                    break;
                case XmlNodeType.ProcessingInstruction:
                    break;
                case XmlNodeType.Comment:
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    break;
                case XmlNodeType.DocumentFragment:
                    break;
                case XmlNodeType.Notation:
                    break;
                case XmlNodeType.Whitespace:
                    break;
                case XmlNodeType.SignificantWhitespace:
                    break;
                case XmlNodeType.EndElement:
                    break;
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.XmlDeclaration:
                    break;
                default:
                    break;
            }
        }

    }

    public static void ForwardInsertEdit(PackFile packFile, XMatch match, PackFileChangeSet changeSet, XPathLookup lookup)
    {
        List<XNode> newNodes = match.nodes;
        XNode? previousNode;
        XNode? nextNode = newNodes.Last().NextNode;

        newNodes.RemoveAt(0);
        newNodes.RemoveAt(newNodes.Count - 1);
        bool isTextInsert = nextNode != null && nextNode.NodeType == XmlNodeType.Text;

        foreach (XNode node in newNodes)
        {
            string nodePath = lookup.LookupPath(node);
            //if (node.Parent != null) node.Remove();
            switch (node.NodeType)
            {
                case XmlNodeType.Text:

                    //packFile.Editor.QueueInsertText(lookup.LookupPath(node), ((XText)node).Value);
                    if (!isTextInsert)
                    {
                        changeSet.AddChange(new AppendTextChange(nodePath, ((XText)node).Value));
                        break;
                    }

                    StringBuilder previousTextBuilder = new();
                    StringBuilder bufferTextBuilder = new();
                    bool skipText = false;
                    previousNode = node.PreviousNode?.PreviousNode;
                    while (previousNode != null)
                    {
                        if (previousNode.NodeType == XmlNodeType.Comment)
                        {
                            XComment comment = (XComment)previousNode;
                            if (comment.Value.Contains("close", StringComparison.OrdinalIgnoreCase))
                            {
                                skipText = true;
                            }
                            else if (comment.Value.Contains("open", StringComparison.OrdinalIgnoreCase))
                            {
                                skipText = false;
                                _ = previousTextBuilder.Insert(0, bufferTextBuilder);
                                bufferTextBuilder = bufferTextBuilder.Clear();
                            }
                            else if (comment.Value.Contains("original", StringComparison.OrdinalIgnoreCase))
                            {
                                skipText = false;
                                bufferTextBuilder = bufferTextBuilder.Clear();
                            }
                            previousNode = previousNode.PreviousNode;
                            continue;
                        }
                        if (skipText)
                        {
                            _ = bufferTextBuilder.Insert(0, '\n');
                            _ = bufferTextBuilder.Insert(0, previousNode.ToString());
                            previousNode = previousNode.PreviousNode;
                            continue;
                        }
                        _ = previousTextBuilder.Insert(0, '\n');
                        _ = previousTextBuilder.Insert(0, previousNode.ToString());
                        previousNode = previousNode.PreviousNode;
                    }

                    string preText = previousTextBuilder.ToString();
                    changeSet.AddChange(new InsertTextChange(nodePath, preText, ((XText)node).Value));

                    //lock (packFile.edits) packFile.edits.AddChange(new InsertTextChange(nodePath, ((XText)node).Value, modInfo));
                    break;
                case XmlNodeType.Element:
                    //packFile.Editor.QueueInsertElement(lookup.LookupPath(node), (XElement)node);
                    lock (packFile.Dispatcher)
                    {
                        if (packFile.Map.PathExists(nodePath))
                        {
                            changeSet.AddChange(new InsertElementChange(nodePath, (XElement)node));
                            //packFile.edits.AddChange(new InsertElementChange(nodePath, (XElement)node, modInfo));
                        }
                        else
                        {
                            changeSet.AddChange(new AppendElementChange(nodePath[..nodePath.LastIndexOf('/')], (XElement)node));
                            //packFile.edits.AddChange(new AppendElementChange(nodePath.Substring(0, nodePath.LastIndexOf('/')), (XElement)node, modInfo));
                        }
                    }
                    break;
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Attribute:
                    break;
                case XmlNodeType.CDATA:
                    break;
                case XmlNodeType.EntityReference:
                    break;
                case XmlNodeType.Entity:
                    break;
                case XmlNodeType.ProcessingInstruction:
                    break;
                case XmlNodeType.Comment:
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    break;
                case XmlNodeType.DocumentFragment:
                    break;
                case XmlNodeType.Notation:
                    break;
                case XmlNodeType.Whitespace:
                    break;
                case XmlNodeType.SignificantWhitespace:
                    break;
                case XmlNodeType.EndElement:
                    break;
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.XmlDeclaration:
                    break;
                default:
                    break;
            }
        }
    }

    public bool MatchReplacePattern(PackFile packFile, List<XNode> nodes, PackFileChangeSet changeSet, XPathLookup lookup)
    {
        XMatchCollection matchCollection = this.replacePattern.Matches(nodes);
        if (!matchCollection.Success)
        {
            return false;
        }

        foreach (XMatch match in matchCollection)
        {
            ForwardReplaceEdit(packFile, match, changeSet, lookup);
        }
        return true;
    }

    public bool MatchInsertPattern(PackFile packFile, List<XNode> nodes, PackFileChangeSet changeSet, XPathLookup lookup)
    {
        XMatchCollection matchCollection = this.insertPattern.Matches(nodes);
        if (!matchCollection.Success)
        {
            return false;
        }

        foreach (XMatch match in matchCollection)
        {
            ForwardInsertEdit(packFile, match, changeSet, lookup);
        }
        return true;
    }

    public void AssembleAnimDataPatch(DirectoryInfo folder)
    {
        foreach (DirectoryInfo subFolder in folder.GetDirectories())
        {
            PandoraConverter.TryGenerateAnimDataPatchFile(subFolder);
        }
        this.pandoraConverter.Assembler.AssembleAnimDataPatch(folder);
    }
    public void AssembleAnimSetDataPatch(DirectoryInfo directoryInfo)
    {
        this.pandoraConverter.Assembler.AssembleAnimSetDataPatch(directoryInfo);
    }
    private PackFileChangeSet AssemblePackFileChanges(PackFile packFile, IModInfo modInfo, DirectoryInfo folder)
    {
        FileInfo[] editFiles = folder.GetFiles("#*.txt");
        PackFileChangeSet changeSet = new(modInfo);
        string modName = modInfo.Name;
        XPathLookup lookup = new();
        foreach (FileInfo editFile in editFiles)
        {
            _ = new List<XNode>();
            string nodeName = Path.GetFileNameWithoutExtension(editFile.Name);
            XElement element;
            try
            {
                element = XElement.Load(editFile.FullName);
            }
            catch (XmlException e)
            {
                Logger.Error($"Nemesis Assembler > File {editFile.FullName} > Load > FAILED > {e.Message}");
                continue;
            }
            List<XNode> nodes = lookup.MapFromElement(element);

            lock (packFile)
            {
                packFile.MapNode(nodeName);
            }

            bool hasInserts = this.MatchInsertPattern(packFile, nodes, changeSet, lookup);
            bool hasReplacements = this.MatchReplacePattern(packFile, nodes, changeSet, lookup);

            if (!hasReplacements && !hasInserts)
            {
                if (packFile.Map.PathExists(modName))
                {
                    Logger.Error($"Nemesis Assembler > File {editFile.FullName} >  No Edits Found > Load > SKIPPED");
                    continue;
                }
                changeSet.AddChange(new PushElementChange(PackFile.ROOT_CONTAINER_NAME, element));
            }
        }
        return changeSet;
    }
    private bool AssemblePackFilePatch(DirectoryInfo folder, Project project, IModInfo modInfo)
    {
        PackFile targetPackFile;
        if (!this.ProjectManager.TryActivatePackFile(folder.Name, project, out targetPackFile!))
        {
            return false;
        }
        lock (targetPackFile)
        {
            targetPackFile.Dispatcher.AddChangeSet(this.AssemblePackFileChanges(targetPackFile, modInfo, folder));
        }

        return true;
    }
    private bool AssemblePackFilePatch(DirectoryInfo folder, IModInfo modInfo)
    {
        PackFile targetPackFile;
        if (!this.ProjectManager.TryActivatePackFile(folder.Name, out targetPackFile!))
        {
            Project targetProject;
            if (!this.ProjectManager.TryLookupProjectFolder(folder.Name, out targetProject!)) { return false; }

            DirectoryInfo[] subFolders = folder.GetDirectories();
            foreach (DirectoryInfo subFolder in subFolders)
            {
                _ = this.AssemblePackFilePatch(subFolder, targetProject, modInfo);
            }
            return true;
        }
        lock (targetPackFile)
        {
            targetPackFile.Dispatcher.AddChangeSet(this.AssemblePackFileChanges(targetPackFile, modInfo, folder));
        }

        return true;
    }

    public List<(FileInfo inFile, FileInfo outFile)> GetExportFiles()
    {
        List<(FileInfo inFile, FileInfo outFile)> exportFiles = new();
        foreach (PackFile packFile in this.ProjectManager.ActivePackFiles)
        {
            exportFiles.Add((packFile.InputHandle, new FileInfo(Path.Join(Directory.GetCurrentDirectory(), packFile.InputHandle.Name))));
        }

        return exportFiles;
    }

}
