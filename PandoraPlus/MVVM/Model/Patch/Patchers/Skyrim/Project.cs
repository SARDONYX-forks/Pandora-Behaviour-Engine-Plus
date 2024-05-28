using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Pandora.Patch.Patchers.Skyrim.AnimData;
using Pandora.Patch.Patchers.Skyrim.Hkx;
using XmlCake.Linq;

namespace Pandora.Core.Patchers.Skyrim;

public class Project
{

    private Dictionary<string, PackFile> filesByName { get; set; } = new Dictionary<string, PackFile>();

    public string Identifier { get; private set; } = string.Empty;

    public bool Valid { get; private set; }

    public PackFile ProjectFile { get; private set; }

    public DirectoryInfo? ProjectDirectory => this.ProjectFile?.InputHandle.Directory;

    public DirectoryInfo? OutputDirectory => this.ProjectFile?.OutputHandle.Directory;

    public DirectoryInfo? OutputBehaviorDirectory => this.BehaviorFile?.OutputHandle.Directory;

    public DirectoryInfo? OutputAnimationDirectory => new(Path.Join(this.OutputDirectory?.FullName, "animations"));

    public PackFileCharacter CharacterFile { get; private set; }
    public PackFile SkeletonFile { get; private set; }
    public PackFileGraph BehaviorFile { get; private set; }

    public ProjectAnimData? AnimData { get; set; }

    public Project()
    {
        this.Valid = false;
    }
    public Project(PackFile projectFile)
    {
        this.Valid = false;
        this.ProjectFile = projectFile;
        this.Identifier = Path.GetFileNameWithoutExtension(this.ProjectFile.InputHandle.Name);
    }
    public Project(PackFile projectfile, PackFileCharacter characterfile, PackFile skeletonfile, PackFileGraph behaviorfile)
    {
        this.ProjectFile = projectfile;
        this.CharacterFile = characterfile;
        this.SkeletonFile = skeletonfile;
        this.BehaviorFile = behaviorfile;

        this.Identifier = Path.GetFileNameWithoutExtension(this.ProjectFile.InputHandle.Name);
        this.Valid = true;
    }

    public PackFile LookupPackFile(string name)
    {
        return this.filesByName[name];
    }

    public bool TryLookupPackFile(string name, out PackFile? packFile)
    {
        return this.filesByName.TryGetValue(name, out packFile);
    }

    public bool ContainsPackFile(string name)
    {
        return this.filesByName.ContainsKey(name);
    }

    public List<string> MapFiles(PackFileCache cache)
    {
        DirectoryInfo? behaviorFolder = this.BehaviorFile.InputHandle.Directory;
        if (behaviorFolder == null)
        {
            return new List<string>();
        }

        FileInfo[] behaviorFiles = behaviorFolder.GetFiles("*.hkx");

        lock (this.filesByName)
        {
            foreach (FileInfo behaviorFile in behaviorFiles)
            {
                PackFileGraph packFile = cache.LoadPackFileGraph(behaviorFile, this);

                //packFile.DeleteExistingOutput();
                this.filesByName.Add(packFile.Name, packFile);
            }

            if (!this.filesByName.ContainsKey(this.SkeletonFile.Name))
            {
                this.filesByName.Add(this.SkeletonFile.Name, this.SkeletonFile);
            }

            if (!this.filesByName.ContainsKey(this.CharacterFile.Name))
            {
                this.filesByName.Add(this.CharacterFile.Name, this.CharacterFile);
            }

            this.filesByName.Add($"{this.Identifier}_skeleton", this.SkeletonFile);
            this.filesByName.Add($"{this.Identifier}_character", this.CharacterFile);

            //SkeletonFile.DeleteExistingOutput();
            //CharacterFile.DeleteExistingOutput();

            return this.filesByName.Keys.ToList();
        }
    }
    public static Project Create(PackFile projectFile, PackFileCache cache)
    {
        if (!projectFile.InputHandle.Exists)
        {
            return new Project();
        }

        PackFileCharacter characterFile = GetCharacterFile(projectFile, cache);
        if (!characterFile.InputHandle.Exists)
        {
            return new Project();
        }

        (PackFile skeleton, PackFileGraph behavior) = GetSkeletonAndBehaviorFile(projectFile, characterFile, cache);

        PackFile skeletonFile = skeleton;
        if (!skeletonFile.InputHandle.Exists)
        {
            return new Project();
        }

        PackFileGraph behaviorFile = behavior;
        if (!behaviorFile.InputHandle.Exists)
        {
            return new Project();
        }

        Project project = new(projectFile, characterFile, skeletonFile, behaviorFile);

        projectFile.ParentProject = project;
        characterFile.ParentProject = project;
        skeletonFile.ParentProject = project;
        behaviorFile.ParentProject = project;

        return project;
    }
    public bool Load(PackFileCache cache)
    {
        if (!this.ProjectFile.InputHandle.Exists)
        {
            return false;
        }

        this.ProjectFile = this.ProjectFile;
        this.CharacterFile = GetCharacterFile(this.ProjectFile, cache);
        if (!this.CharacterFile.InputHandle.Exists)
        {
            return false;
        }

        (PackFile skeleton, PackFileGraph behavior) = GetSkeletonAndBehaviorFile(this.ProjectFile, this.CharacterFile, cache);

        this.SkeletonFile = skeleton;
        if (!this.SkeletonFile.InputHandle.Exists)
        {
            return false;
        }

        this.BehaviorFile = behavior;
        if (!this.BehaviorFile.InputHandle.Exists)
        {
            return false;
        }

        this.ProjectFile.ParentProject = this;
        this.CharacterFile.ParentProject = this;
        this.SkeletonFile.ParentProject = this;
        this.BehaviorFile.ParentProject = this;

        return true;
    }
    public static Project Load(FileInfo file, PackFileCache cache)
    {
        return Create(cache.LoadPackFile(file), cache);
    }

    //public static Project Load(string projectFilePath) => Load(new PackFile(projectFilePath));

    private static PackFileCharacter GetCharacterFile(PackFile projectFile, PackFileCache cache)
    {

        XMap projectMap = projectFile.Map;
        XElement projectData = projectFile.GetFirstNodeOfClass("hkbProjectStringData");
        projectMap.MapSlice(projectData, true);

        string characterFilePath = projectMap.Lookup("characterFilenames").Value.ToLower();

        return cache.LoadPackFileCharacter(new FileInfo(Path.Combine(projectFile.InputHandle.DirectoryName!, characterFilePath)));
    }

    private static (PackFile skeleton, PackFileGraph behavior) GetSkeletonAndBehaviorFile(PackFile projectFile, PackFileCharacter characterFile, PackFileCache cache)
    {
        XMap characterMap = characterFile.Map;

        string skeletonFilePath = characterMap.Lookup(characterFile.RigNamePath).Value;

        string behaviorFilePath = characterMap.Lookup(characterFile.BehaviorFilenamePath).Value;

        return (cache.LoadPackFile(new FileInfo(Path.Combine(projectFile.InputHandle.DirectoryName!, skeletonFilePath))), cache.LoadPackFileGraph(new FileInfo(Path.Combine(projectFile.InputHandle.DirectoryName!, behaviorFilePath))));

    }

}
