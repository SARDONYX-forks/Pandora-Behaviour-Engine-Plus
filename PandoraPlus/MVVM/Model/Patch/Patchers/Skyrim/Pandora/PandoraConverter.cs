using System.IO;
using Pandora.Core.Patchers.Skyrim;
using Pandora.Patch.Patchers.Skyrim.AnimData;
using Pandora.Patch.Patchers.Skyrim.AnimSetData;
using Pandora.Patch.Patchers.Skyrim.Hkx;

namespace Pandora.Patch.Patchers.Skyrim.Pandora;

public class PandoraConverter
{

    public PandoraAssembler Assembler { get; private set; }

    public PandoraConverter(ProjectManager projManager, AnimSetDataManager animSDManager, AnimDataManager animDManager)
    {
        this.Assembler = new PandoraAssembler(projManager, animSDManager, animDManager);
    }

    public static void TryGraphInjection(DirectoryInfo folder, PackFile packFile, PackFileChangeSet changeSet)
    {
        DirectoryInfo injectFolder = new($"{folder.FullName}\\inject");
        if (!injectFolder.Exists) { return; }

        //Assembler.AssembleGraphInjection(injectFolder, packFile, changeSet);
    }

    public static void TryGenerateAnimDataPatchFile(DirectoryInfo folder)
    {
        DirectoryInfo? parentFolder = folder.Parent;
        if (parentFolder == null)
        {
            return;
        }

        FileInfo patchFile = new($"{parentFolder.FullName}\\{folder.Name.Split('~')[0]}.txt");
        if (patchFile.Exists)
        {
            return;
        }

        using FileStream writeStream = patchFile.OpenWrite();
        using StreamWriter writer = new(writeStream);
        FileInfo[] files = folder.GetFiles();
        foreach (FileInfo file in files)
        {
            string clipName = file.Name.Split('~')[0];

            if (clipName.Contains('$'))
            {
                continue;
            }

            writer.WriteLine(clipName);

        }

    }
}
