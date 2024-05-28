using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using HKX2;
using Pandora.Core;
using Pandora.Core.Patchers.Skyrim;
using Pandora.Patch.Patchers.Skyrim.Hkx;

namespace Pandora.Patch.Patchers.Skyrim.FNIS;
public class FNISParser
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly HashSet<string> animTypePrefixes = new() { "b", "s", "so", "fu", "fuo", "+", "ofa", "pa", "km", "aa", "ch" };

    private static readonly Regex hkxRegex = new("\\S*\\.hkx", RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, string> stateMachineMap = new()
    {
        {"atronachflame~atronachflamebehavior", "#0414" },
        {"atronachfrostproject~atronachfrostbehavior", "#0439" },
        {"atronachstormproject~atronachstormbehavior", "#0369" },
        {"bearproject~bearbehavior", "#0151" },
        {"chaurusproject~chaurusbehavior", "#0509" },
        {"dogproject~dogbehavior", "#0144" },
        {"wolfproject~wolfbehavior", "#0169" },
        {"defaultfemale~0_master", "#0340" },
        {"defaultmale~0_master", "#0340" },
        {"firstperson~0_master", "#0167" },
        {"spherecenturion~scbehavior", "#0780" },
        {"dwarvenspidercenturionproject~dwarvenspiderbehavior", "#0394" },
        {"steamproject~steambehavior", "#0538" },
        {"falmerproject~falmerbehavior", "#1294" },
        {"frostbitespiderproject~frostbitespiderbehavior", "#0402" },
        {"giantproject~giantbehavior", "#0795" },
        {"goatproject~goatbehavior", "#0140" },
        {"highlandcowproject~h-cowbehavior", "#0152" },
        {"deerproject~deerbehavior", "#0145" },
        {"dragonproject~dragonbehavior", "#1610" },
        {"dragon_priest~dragon_priest", "#0758" },
        {"draugrproject~draugrbehavior", "#1998" },
        {"draugrskeletonproject~draugrbehavior", "#1998" },
        {"hagravenproject~havgravenbehavior", "#0634" },
        {"horkerproject~horkerbehavior", "#0161" },
        {"horseproject~horsebehavior", "#0493" },
        {"icewraithproject~icewraithbehavior", "#0262" },
        {"mammothproject~mammothbehavior", "#0155" },
        {"mudcrabproject~mudcrabbehavior", "#0481" },
        {"sabrecatproject~sabrecatbehavior", "#0140" },
        {"skeeverproject~skeeverbehavior", "#0132" },
        {"slaughterfishproject~slaughterfishbehavior", "#0128" },
        {"spriggan~sprigganbehavior", "#0610" },
        {"trollproject~trollbehavior", "#0708" },
        {"vampirelord~vampirelord", "#1114" },
        {"werewolfbeastproject~werewolfbehavior", "#1207" },
        {"wispproject~wispbehavior", "#0410" },
        {"witchlightproject~witchlightbehavior", "#0152" },
        {"chickenproject~chickenbehavior", "#0322" },
        {"hareproject~harebehavior", "#0297" },
        {"chaurusflyer~chaurusflyerbehavior", "#0402" },
        {"vampirebruteproject~vampirebrutebehavior", "#0502" },
        {"benthiclurkerproject~benthiclurkerbehavior", "#0708" },
        {"boarriekling~boarbehavior", "#0548" },
        {"ballistacenturion~bcbehavior", "#0475" },
        {"hmdaedra~hmdaedra", "#0490" },
        {"netchproject~netchbehavior", "#0279" },
        {"rieklingproject~rieklingbehavior", "#0730" },
        {"scribproject~scribbehavior", "#0578" }
    };

    private static readonly Dictionary<string, string> linkedCharacterMap = new()
    {
        {"defaultmale", "defaultfemale" },
        {"defaultfemale", "defaultmale" },
        {"draugrskeletonproject", "draugrproject" },
        {"draugrproject", "draugrskeletonproject" },
        {"wolfproject", "dogproject" },
        {"dogproject", "wolfproject" }
    };

    private static readonly Dictionary<string, string> animListExcludeMap = new()
    {
        {"dogproject", "wolf" },
        {"wolfproject", "dog" }
    };

    private readonly HashSet<string> parsedFolders = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<FNISModInfo> ModInfos { get; private set; } = new HashSet<FNISModInfo>();

    public FNISParser(ProjectManager manager)
    {
        this.projectManager = manager;
    }
    private ProjectManager projectManager { get; }
    public void ScanProjectAnimlist(Project project)
    {
        DirectoryInfo? animationsFolder = project.OutputAnimationDirectory;
        DirectoryInfo? behaviorFolder = project.OutputBehaviorDirectory;
        if (animationsFolder == null || behaviorFolder == null || !behaviorFolder.Exists) { return; }
        lock (this.parsedFolders)
        {
            if (this.parsedFolders.Contains(behaviorFolder.FullName)) { return; }

            _ = this.parsedFolders.Add(animationsFolder.FullName);
            _ = this.parsedFolders.Add(behaviorFolder.FullName);
        }
        FileInfo[] modFiles = behaviorFolder.GetFiles("FNIS*.hkx");

        if (modFiles.Length > 0) { _ = this.projectManager.ActivatePackFile(project.BehaviorFile); }

        foreach (FileInfo modFile in modFiles)
        {
            _ = this.InjectGraphReference(modFile, project.BehaviorFile);
        }
        if (!animationsFolder.Exists) { return; }
        DirectoryInfo[] modAnimationFolders = animationsFolder.GetDirectories();

        if (modAnimationFolders.Length == 0) { return; }

        _ = Parallel.ForEach(modAnimationFolders, folder => { this.ParseAnimlistFolder(folder, project, this.projectManager); });
    }
    private bool InjectGraphReference(FileInfo sourceFile, PackFileGraph destPackFile)
    {
        string stateFolderName;

        if (!stateMachineMap.TryGetValue(destPackFile.UniqueName, out stateFolderName!)) { return false; }

        destPackFile.MapNode(stateFolderName);

        string nameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFile.Name);
        string refName = nameWithoutExtension.Replace(' ', '_');
        string stateInfoPath = string.Format("{0}/states", stateFolderName);
        string graphPath = $"{destPackFile.OutputHandle.Directory?.Name}\\{nameWithoutExtension}.hkx";
        FNISModInfo modInfo = new(sourceFile);
        lock (this.ModInfos)
        {
            _ = this.ModInfos.Add(modInfo);
        }

        PackFileChangeSet changeSet = new(modInfo);

        PatchNodeCreator nodeMaker = new(changeSet.Origin.Code);

        hkbBehaviorReferenceGenerator behaviorRef = nodeMaker.CreateBehaviorReferenceGenerator(refName, graphPath, out string behaviorRefName);
        XElement behaviorRefElement = nodeMaker.TranslateToLinq(behaviorRef, behaviorRefName);

        hkbStateMachineStateInfo stateInfo = nodeMaker.CreateSimpleStateInfo(behaviorRef, out string stateInfoName);
        XElement stateInfoElement = nodeMaker.TranslateToLinq(stateInfo, stateInfoName);

        changeSet.AddChange(new AppendElementChange(PackFile.ROOT_CONTAINER_NAME, behaviorRefElement));
        changeSet.AddChange(new AppendElementChange(PackFile.ROOT_CONTAINER_NAME, stateInfoElement));
        changeSet.AddChange(new AppendTextChange(stateInfoPath, stateInfoName));

        destPackFile.Dispatcher.AddChangeSet(changeSet);
        return true;
    }
    private void ParseAnimlistFolder(DirectoryInfo folder, Project project, ProjectManager projectManager)
    {
        FileInfo[] animlistFiles = folder.GetFiles("*list.txt");

        if (animListExcludeMap.TryGetValue(project.Identifier, out string? excludeName))
        {
            animlistFiles = animlistFiles.Where(f => !f.Name.EndsWith(excludeName)).ToArray();
        }

        if (animlistFiles.Length == 0) { return; }

        List<PackFileCharacter> characterFiles = new() { project.CharacterFile };

        if (linkedCharacterMap.TryGetValue(project.Identifier, out string? linkedProjectIdentifier) && projectManager.TryGetProject(linkedProjectIdentifier, out Project? linkedProject))
        {
            characterFiles.Add(linkedProject!.CharacterFile);

        }
        foreach (PackFileCharacter characterFile in characterFiles) { _ = projectManager.ActivatePackFile(characterFile); }
        foreach (FileInfo animlistFile in animlistFiles)
        {
            ParseAnimlist(animlistFile, characterFiles);
        }
    }
    private static void ParseAnimlist(FileInfo file, List<PackFileCharacter> characterPackFiles)
    {

        List<PackFileChangeSet> changeSets = new();

        for (int i = 0; i < characterPackFiles.Count; i++) { changeSets.Add(new PackFileChangeSet(new FNISModInfo(file))); }

        DirectoryInfo? parentFolder = file.Directory;
        if (parentFolder == null) { return; }

        DirectoryInfo? ancestorFolder = parentFolder.Parent;
        if (ancestorFolder == null) { return; }

        string root = Path.Combine(ancestorFolder.Name, parentFolder.Name);

        using (FileStream readStream = file.OpenRead())
        {
            using StreamReader reader = new(readStream);
            string? expectedLine;
            while ((expectedLine = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(expectedLine) || expectedLine[0] == '\'') { continue; }

                Match match = hkxRegex.Match(expectedLine);
                if (!match.Success) { continue; }

                string animationPath = Path.Combine(root, match.Value);
                for (int i = 0; i < changeSets.Count; i++)
                {
                    changeSets[i].AddChange(new AppendElementChange(characterPackFiles[i].AnimationNamesPath, new XElement("hkcstring", animationPath)));
                }

            }
        }
        for (int i = 0; i < characterPackFiles.Count; i++)
        {
            characterPackFiles[i].Dispatcher.AddChangeSet(changeSets[i]);
        }

    }

}
