using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pandora.Core;
using Pandora.Core.Patchers;
using Pandora.Patch.IOManagers;
using Pandora.Patch.IOManagers.Skyrim;
using Pandora.Patch.Patchers.Skyrim.Hkx;
using Pandora.Patch.Patchers.Skyrim.Nemesis;
using Pandora.Patch.Patchers.Skyrim.Pandora;
using PatcherFlags = Pandora.Core.Patchers.IPatcher.PatcherFlags;

namespace Pandora.Patch.Patchers.Skyrim;
public class SkyrimPatcher : IPatcher
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private List<IModInfo> activeMods { get; set; } = new List<IModInfo>();

    public void SetTarget(List<IModInfo> mods)
    {
        this.activeMods = mods;
    }

    private readonly Exporter<PackFile> exporter = new PackFileExporter();

    private NemesisAssembler nemesisAssembler { get; set; }

    private PandoraAssembler pandoraAssembler { get; set; }

    public PatcherFlags Flags { get; private set; } = PatcherFlags.None;

    private static readonly Version currentVersion = new(1, 4, 2);

    private static readonly string versionLabel = "alpha";
    public string GetVersionString()
    {
        return $"{currentVersion}-{versionLabel}";
    }

    public Version GetVersion()
    {
        return currentVersion;
    }

    public SkyrimPatcher()
    {
        this.nemesisAssembler = new NemesisAssembler();
        this.pandoraAssembler = new PandoraAssembler(this.nemesisAssembler);
    }
    public SkyrimPatcher(Exporter<PackFile> manager)
    {
        this.exporter = manager;
        this.nemesisAssembler = new NemesisAssembler(manager);
        this.pandoraAssembler = new PandoraAssembler(this.nemesisAssembler);
    }
    public string GetPostRunMessages()
    {
        StringBuilder logBuilder;
        logBuilder = new StringBuilder("Resources loaded successfully.\r\n\r\n");

        for (int i = 0; i < this.activeMods.Count; i++)
        {
            IModInfo mod = this.activeMods[i];
            string modLine = $"Pandora Mod {i + 1} : {mod.Name} - v.{mod.Version}";
            _ = logBuilder.AppendLine(modLine);
            logger.Info(modLine);
        }

        this.nemesisAssembler.GetPostMessages(logBuilder);

        return logBuilder.ToString();
    }

    public string GetFailureMessages()
    {
        StringBuilder logBuilder;
        logBuilder = new StringBuilder("CRITICAL FAILURE \r\n\r\n");

        if (this.Flags.HasFlag(PatcherFlags.UpdateFailed)) { _ = logBuilder.AppendLine("Engine had one or more errors while updating."); }

        _ = logBuilder.Append("If the cause is unknown: submit a report to the author of the engine and attach Engine.log");

        return logBuilder.ToString();
    }

    public void Run()
    {
        //assembler.ApplyPatches();
    }
    public async Task<bool> RunAsync()
    {
        return await this.nemesisAssembler.ApplyPatchesAsync();
    }

    public async Task<bool> UpdateAsync()
    {

        logger.Info($"Skyrim Patcher {this.GetVersionString()}");

        //Parallel.ForEach(activeMods, mod => { assembler.AssemblePatch(mod); });

        try
        {
            _ = Parallel.ForEach(this.activeMods, mod =>
            {
                switch (mod.Format)
                {
                    case IModInfo.ModFormat.Nemesis:
                        this.nemesisAssembler.AssemblePatch(mod);
                        break;
                    case IModInfo.ModFormat.Pandora:
                        this.pandoraAssembler.AssemblePatch(mod);
                        break;
                    case IModInfo.ModFormat.FNIS:
                        break;
                    default:
                        break;
                }
            }
            );
        }
        catch (Exception ex)
        {
            this.Flags |= PatcherFlags.UpdateFailed;
            logger.Fatal($"Skyrim Patcher > Active Mods > Update > FAILED > {ex}");
        }

        //await assembler.LoadResourcesAsync();

        //List<Task> assembleTasks = new List<Task>();
        //foreach (var mod in activeMods)
        //{
        //	assembleTasks.Add(Task.Run(() => { assembler.AssemblePatch(mod); }));
        //}
        //await Task.WhenAll(assembleTasks);

        return !this.Flags.HasFlag(PatcherFlags.UpdateFailed);
    }

    public static async Task WriteAsync()
    {

    }

    public void Update()
    {

    }

    public async Task PreloadAsync()
    {
        await this.nemesisAssembler.LoadResourcesAsync();
    }

    public void SetOutputPath(DirectoryInfo directoryInfo)
    {
        this.exporter.ExportDirectory = directoryInfo;
    }
}
