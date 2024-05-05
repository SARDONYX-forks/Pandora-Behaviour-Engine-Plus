using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Pandora.Patch.Patchers.Skyrim.FNIS;
using Pandora.Patch.Patchers.Skyrim.Hkx;

namespace Pandora.Core.Patchers.Skyrim;

public class ProjectManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private Dictionary<string, Project> projectMap { get; set; } = new Dictionary<string, Project>();
    private Dictionary<string, Project> fileProjectMap { get; set; } = new Dictionary<string, Project>();

    private Dictionary<string, Project> folderProjectMap { get; set; } = new Dictionary<string, Project>();

    private DirectoryInfo templateFolder { get; set; }

    private DirectoryInfo outputFolder { get; set; }

    private PackFileCache packFileCache { get; set; } = new PackFileCache();
    public HashSet<PackFile> ActivePackFiles { get; private set; } = new HashSet<PackFile>();

    private readonly FNISParser fnisParser;

    private readonly bool CompleteExportSuccess = true;

    public ProjectManager(DirectoryInfo templateFolder, DirectoryInfo outputFolder)
    {
        this.templateFolder = templateFolder;
        this.outputFolder = outputFolder;
        this.fnisParser = new FNISParser(this);

    }
    public void GetExportInfo(StringBuilder builder)
    {
        if (this.CompleteExportSuccess) { return; }
        _ = builder.AppendLine();

        foreach (PackFile? failedPackFile in this.ActivePackFiles.Where(pf => !pf.ExportSuccess))
        {
            _ = builder.AppendLine($"FATAL ERROR: Could not export {failedPackFile.UniqueName}. Check Engine.log for more information.");
        }
    }
    public void GetAnimationInfo(StringBuilder builder)
    {
        Dictionary<string, Project>.ValueCollection projects = this.projectMap.Values;
        uint totalAnimationCount = 0;

        _ = builder.AppendLine();
        foreach (Project project in projects)
        {
            uint animCount = project.CharacterFile.NewAnimationCount;
            if (animCount == 0) { continue; }
            totalAnimationCount += animCount;
            _ = builder.AppendLine($"{animCount} animations added to {project.Identifier}.");
        }
        _ = builder.AppendLine();
        _ = builder.AppendLine($"{totalAnimationCount} total animations added.");
    }

    public void GetFNISInfo(StringBuilder builder)
    {
        uint fnisModCount = 0;
        _ = builder.AppendLine();
        foreach (IModInfo modInfo in this.fnisParser.ModInfos)
        {
            fnisModCount++;
            _ = builder.AppendLine($"FNIS Mod {fnisModCount} : {modInfo.Name}");
            Logger.Info($"FNIS Mod {fnisModCount} : {modInfo.Name}");
        }
    }

    public bool TryGetProject(string name, out Project? project)
    {
        return this.projectMap.TryGetValue(name, out project);
    }

    public bool ProjectExists(string name)
    {
        return this.projectMap.ContainsKey(name);
    }

    public void LoadTrackedProjects()
    {
        FileInfo projectList = new($"{this.templateFolder.FullName}\\vanilla_projectpaths.txt");
        string? expectedLine = null;
        List<string> projectPaths = new();
        using (FileStream readStream = projectList.OpenRead())
        {
            using StreamReader streamReader = new(readStream);
            while ((expectedLine = streamReader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(expectedLine))
                {
                    continue;
                }

                projectPaths.Add(expectedLine);

            }
        }
        this.LoadProjects(projectPaths);
    }
    public void LoadProjects(List<string> projectPaths)
    {
        foreach (string projectPath in projectPaths)
        {
            Project? project = this.LoadProject(projectPath);
            if (project == null || !project.Valid)
            {
                continue;
            }

            this.ExtractProject(project);
        }
    }
    public void LoadProjectsParallel(List<string> projectPaths)
    {
        List<Project> projects = new();
        List<List<Project>> projectChunks = new();
        foreach (string projectPath in projectPaths)
        {
            Project? project = this.LoadProjectHeader(projectPath);
            if (project == null)
            {
                continue;
            }

            projects.Add(project);
        }
        List<Project> buffer = new();
        for (int i = 0; i < projects.Count; i++)
        {
            buffer.Add(projects[i]);
            if ((i % 10 == 0 && i > 0) || i == projects.Count - 1)
            {
                List<Project> chunk = new();
                foreach (Project project in buffer) { chunk.Add(project); }
                projectChunks.Add(chunk);
                buffer.Clear();
            }
        }
        foreach (List<Project> chunk in projectChunks)
        {
            _ = Parallel.ForEach(chunk, project =>
            {
                _ = project.Load(this.packFileCache);
            });
        }
        //Partitioner<Project> partitioner = Partitioner.Create(projects);

        //Parallel.ForEach(partitioner, (project, loopstate) =>
        //{
        //	project.Load(packFileCache);
        //});

        foreach (Project project in projects)
        {
            this.ExtractProject(project);
        }
    }
    public void ExtractProjects()
    {
        _ = Parallel.ForEach(this.projectMap.Values, this.ExtractProject);
    }
    public async Task LoadProjectAsync(string projectFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            return;
        }

        Project project = Project.Load(new FileInfo(Path.Join(this.templateFolder.FullName, projectFilePath)), this.packFileCache);

        lock (this.projectMap)
        {
            this.projectMap.Add(project.Identifier, project);
        }

        await Task.Run(() => { this.ExtractProject(project); });
        //ExtractProject(project);

    }
    public Project? LoadProject(string projectFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            return null;
        }

        lock (this.projectMap)
        {

            Project project = Project.Load(new FileInfo(Path.Join(this.templateFolder.FullName, projectFilePath)), this.packFileCache);

            this.projectMap.Add(project.Identifier, project);

            //lock (project) ExtractProject(project);
            return project;
        }

    }
    public Project? LoadProjectHeader(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
        {
            return null;
        }

        lock (this.projectMap)
        {
            Project project = new(this.packFileCache.LoadPackFile(new FileInfo(Path.Join(this.templateFolder.FullName, projectFilePath))));

            this.projectMap.Add(project.Identifier, project);

            return project;
        }
    }

    private void ExtractProject(Project project)
    {
        lock (this.fileProjectMap)
        {
            List<string> fileNames = project.MapFiles(this.packFileCache);
            foreach (string file in fileNames)
            {
                if (!this.fileProjectMap.ContainsKey(file))
                {
                    this.fileProjectMap.Add(file, project);
                }
            }
        }
        lock (this.folderProjectMap)
        {
            if (!this.folderProjectMap.ContainsKey(project.ProjectDirectory!.Name))
            {
                this.folderProjectMap.Add(project.ProjectDirectory!.Name, project);
            }
        }
    }
    private bool TryLookupNestedPackFile(string name, out PackFile? packFile)
    {
        packFile = null;
        string[] sections = name.Split('~');
        Project project;
        return this.projectMap.TryGetValue(sections[0], out project!) && project.TryLookupPackFile(name, out packFile);
    }
    private PackFile LookupNestedPackFile(string name)
    {
        string[] sections = name.Split('~');

        Project targetProject = this.projectMap[sections[0]];
        return targetProject.LookupPackFile(sections[1]);
    }

    private bool ContainsNestedPackFile(string name)
    {
        string[] sections = name.Split('~');

        Project targetProject;

        return this.projectMap.TryGetValue(sections[0], out targetProject!) && targetProject.ContainsPackFile(sections[1]);
    }

    public bool ProjectLoaded(string name)
    {
        return this.projectMap.ContainsKey(name);
    }

    public Project LookupProject(string name)
    {
        return this.projectMap[name];
    }

    public bool TryLookupPackFile(string name, out PackFile? packFile)
    {
        name = name.ToLower();
        packFile = null;
        if (name.Contains('~'))
        {
            return this.TryLookupNestedPackFile(name, out packFile);
        }
        Project project;
        return this.fileProjectMap.TryGetValue(name, out project!) && project.TryLookupPackFile(name, out packFile);
    }
    public PackFile LookupPackFile(string name)
    {
        name = name.ToLower();
        return name.Contains('~') ? this.LookupNestedPackFile(name) : this.fileProjectMap[name].LookupPackFile(name);
    }

    public bool ContainsPackFile(string name)
    {
        return name.Contains('~') ? this.ContainsNestedPackFile(name) : this.fileProjectMap.ContainsKey(name) && this.fileProjectMap[name].ContainsPackFile(name);
    }
    public bool TryLookupProjectFolder(string name, out Project? project)
    {
        return this.folderProjectMap.TryGetValue(name, out project);
    }
    public bool TryActivatePackFile(string name, Project project, out PackFile? packFile)
    {
        if (!project.TryLookupPackFile(name, out packFile))
        {
            return false;
        }
        lock (this.ActivePackFiles)
            lock (packFile)
            {
                if (this.ActivePackFiles.Contains(packFile)) { return true; }
                _ = this.ActivePackFiles.Add(packFile);
                packFile.Activate();
            }
        return true;
    }
    public bool TryActivatePackFile(string name, out PackFile? packFile)
    {
        if (!this.TryLookupPackFile(name, out packFile!))
        {
            return false;
        }
        lock (this.ActivePackFiles)
            lock (packFile)
            {
                if (this.ActivePackFiles.Contains(packFile)) { return true; }
                _ = this.ActivePackFiles.Add(packFile);
                packFile.Activate();
            }
        return true;
    }
    public PackFile ActivatePackFile(string name)
    {

        PackFile packFile = this.LookupPackFile(name);
        lock (this.ActivePackFiles)
            lock (packFile)
            {
                if (this.ActivePackFiles.Contains(packFile))
                {
                    return packFile;
                }

                _ = this.ActivePackFiles.Add(packFile);
                packFile.Activate();
            }
        return packFile;
    }
    public bool ActivatePackFile(PackFile packFile)
    {
        lock (this.ActivePackFiles)
            lock (packFile)
            {
                if (this.ActivePackFiles.Contains(packFile))
                {
                    return true;
                }

                _ = this.ActivePackFiles.Add(packFile);
                packFile.Activate();
            }
        return false;
    }

    public bool ApplyPatches()
    {
        PackFileCache.DeletePackFileOutput();

        _ = Parallel.ForEach(this.ActivePackFiles, packFile =>
        {
            packFile.ApplyChanges();
            //packFile.Map.Save(Path.Join(Directory.GetCurrentDirectory(), packFile.InputHandle.Name));

        });
        //foreach (PackFile packFile in ActivePackFiles)
        //{
        //	packFile.ApplyChanges();
        //	packFile.Map.Save(Path.Join(Directory.GetCurrentDirectory(), packFile.InputHandle.Name));
        //	packFile.Export();
        //}
        return true;
    }

    public async Task<bool> ApplyPatchesParallel()
    {
        Task deleteOutputTask = Task.Run(PackFileCache.DeletePackFileOutput);
        try
        {
            _ = Parallel.ForEach(this.projectMap.Values, this.fnisParser.ScanProjectAnimlist);
        }
        catch (Exception ex)
        {
            Logger.Error($"FNIS Parser > Scan > Failed > {ex.Message}");
        }

        _ = Parallel.ForEach(this.ActivePackFiles, packFile =>
        {
            packFile.ApplyChanges();
        });

        await deleteOutputTask;

        return true;

    }
    public void SaveCache()
    {
        PackFileCache.SavePackFileOutput(this.ActivePackFiles);
    }
}
