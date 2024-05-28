using System;
using System.Collections.Generic;
using System.IO;
using Pandora.Core.Patchers.Skyrim;

namespace Pandora.Patch.Patchers.Skyrim.Hkx;
public class PackFileCache
{
    private readonly Dictionary<string, PackFile> pathMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly FileInfo PreviousOutputFile = new(Directory.GetCurrentDirectory() + "\\Pandora_Engine\\PreviousOutput.txt");

    public PackFile LoadPackFile(FileInfo file)
    {

        PackFile? packFile = null;

        lock (this.pathMap)
        {
            if (!this.pathMap.TryGetValue(file.FullName, out packFile))
            {
                packFile = new PackFile(file);
                this.pathMap.Add(file.FullName, packFile);
            }
        }

        return packFile;
    }

    public PackFileGraph LoadPackFileGraph(FileInfo file)
    {
        PackFile? packFile = null;
        lock (this.pathMap)
        {
            if (!this.pathMap.TryGetValue(file.FullName, out packFile))
            {
                packFile = new PackFileGraph(file);
                this.pathMap.Add(file.FullName, packFile);
            }
        }

        return (PackFileGraph)packFile;
    }

    public PackFileGraph LoadPackFileGraph(FileInfo file, Project project)
    {
        PackFile? packFile = null;
        lock (this.pathMap)
        {
            if (!this.pathMap.TryGetValue(file.FullName, out packFile))
            {
                packFile = new PackFileGraph(file, project);
                this.pathMap.Add(file.FullName, packFile);

            }
        }

        return (PackFileGraph)packFile;
    }

    public PackFileCharacter LoadPackFileCharacter(FileInfo file)
    {
        PackFile? packFile = null;
        lock (this.pathMap)
        {
            if (!this.pathMap.TryGetValue(file.FullName, out packFile))
            {
                packFile = new PackFileCharacter(file);
                this.pathMap.Add(file.FullName, packFile);
            }
        }

        return (PackFileCharacter)packFile;
    }

    public PackFileCharacter LoadPackFileCharacter(FileInfo file, Project project)
    {
        PackFile? packFile = null;
        lock (this.pathMap)
        {
            if (!this.pathMap.TryGetValue(file.FullName, out packFile))
            {
                packFile = new PackFileCharacter(file, project);
                this.pathMap.Add(file.FullName, packFile);
            }
        }

        return (PackFileCharacter)packFile;
    }

    public static void DeletePackFileOutput()
    {
        if (!PreviousOutputFile.Exists) { return; }

        using FileStream readStream = PreviousOutputFile.OpenRead();
        using StreamReader reader = new(readStream);
        string? expectedLine;
        while ((expectedLine = reader.ReadLine()) != null)
        {
            FileInfo file = new(expectedLine);
            if (!file.Exists) { continue; }

            file.Delete();
        }
    }

    public static void SavePackFileOutput(IEnumerable<PackFile> packFiles)
    {
        using FileStream readStream = PreviousOutputFile.OpenWrite();
        using StreamWriter writer = new(readStream);
        foreach (PackFile packFile in packFiles)
        {
            if (!packFile.ExportSuccess || !packFile.OutputHandle.Exists) { continue; }

            writer.WriteLine(packFile.OutputHandle.FullName);
        }
    }
}
