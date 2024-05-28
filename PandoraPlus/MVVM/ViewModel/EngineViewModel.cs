using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pandora.Command;
using Pandora.Core;
using Pandora.Core.Engine.Configs;
using Pandora.MVVM.Data;

namespace Pandora.MVVM.ViewModel;

public class EngineViewModel : INotifyPropertyChanged
{
    private readonly NemesisModInfoProvider nemesisModInfoProvider = new();
    private readonly PandoraModInfoProvider pandoraModInfoProvider = new();

    private string logText = "";
    private readonly HashSet<string> startupArguments = new(StringComparer.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public bool? DialogResult { get; set; } = true;
    private bool closeOnFinish = false;
    private bool autoRun = false;
    public BehaviourEngine Engine { get; private set; } = new BehaviourEngine();

    public RelayCommand LaunchCommand { get; }
    public RelayCommand SetEngineConfigCommand { get; }
    public RelayCommand ExitCommand { get; }

    public ObservableCollection<IModInfo> Mods { get; set; } = new ObservableCollection<IModInfo>();

    public bool LaunchEnabled { get; set; } = true;

    private bool engineRunning = false;

    private readonly FileInfo activeModConfig;

    private readonly Dictionary<string, IModInfo> modsByCode = new();

    private bool modInfoCache = false;

    private static readonly DirectoryInfo currentDirectory = new(Directory.GetCurrentDirectory());

    private Task preloadTask;

    private ObservableCollection<IEngineConfigurationViewModel> engineConfigs = new();
    public ObservableCollection<IEngineConfigurationViewModel> EngineConfigurationViewModels
    {
        get => this.engineConfigs;
        set
        {
            this.engineConfigs = value;
            this.RaisePropertyChanged(nameof(this.EngineConfigurationViewModels));
        }
    }

    public string LogText
    {
        get => this.logText;
        set
        {
            this.logText = value;
            this.RaisePropertyChanged(nameof(this.LogText));
        }
    }
    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public EngineViewModel()
    {
        this.startupArguments = Environment.GetCommandLineArgs().ToHashSet(StringComparer.OrdinalIgnoreCase);
        this.LaunchCommand = new RelayCommand(this.LaunchEngine, this.CanLaunchEngine);
        this.ExitCommand = new RelayCommand(Exit);
        this.SetEngineConfigCommand = new RelayCommand(this.SetEngineConfiguration, this.CanLaunchEngine);
        this.activeModConfig = new FileInfo($"{currentDirectory}\\Pandora_Engine\\ActiveMods.txt");
        CultureInfo culture;

        culture = CultureInfo.CreateSpecificCulture("en-US");

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        this.ReadStartupArguments();
        this.SetupConfigurationOptions();
        this.preloadTask = Task.Run(this.Engine.PreloadAsync);

        if (this.autoRun) { this.LaunchCommand.Execute(null); }

    }
    private void SetupConfigurationOptions()
    {
        this.EngineConfigurationViewModels.Add(
            new EngineConfigurationViewModelContainer("Skyrim SE/AE",
                new EngineConfigurationViewModelContainer("Behavior",

                    new EngineConfigurationViewModelContainer("Patch",
                        new EngineConfigurationViewModel<SkyrimConfiguration>("Normal", this.SetEngineConfigCommand),
                        new EngineConfigurationViewModel<SkyrimDebugConfiguration>("Debug", this.SetEngineConfigCommand)
                    )
                    //,
                    //new EngineConfigurationViewModelContainer("Convert"

                    //),
                    //new EngineConfigurationViewModelContainer("Validate"
                    //)
                    )
                )
            );
        //EngineConfigs.Add(new EngineConfigurationViewModel<SkyrimConfiguration>("Skyrim SE/AE", SetEngineConfigCommand));
        //EngineConfigs.Add(new EngineConfigurationViewModel<SkyrimDebugConfiguration>("Skyrim SE/AE Debug", SetEngineConfigCommand));
    }
    public async Task LoadAsync()
    {

        List<IModInfo> modInfos = new();

        modInfos.AddRange(await this.nemesisModInfoProvider?.GetInstalledMods(currentDirectory + "\\Nemesis_Engine\\mod")!);
        modInfos.AddRange(await this.pandoraModInfoProvider?.GetInstalledMods(currentDirectory + "\\Pandora_Engine\\mod")!);

        for (int i = 0; i < modInfos.Count; i++)
        {
            IModInfo? modInfo = modInfos[i];
            //Mods.Add(modInfo);
            if (this.modsByCode.TryGetValue(modInfo.Code, out IModInfo? existingModInfo))
            {
                logger.Warn($"Engine > Folder {modInfo.Folder.Parent?.Name} > Parse Info > {modInfo.Code} Already Exists > SKIPPED");
                modInfos.RemoveAt(i);
                continue;
            }
            this.modsByCode.Add(modInfo.Code, modInfo);
        }

        this.modInfoCache = this.LoadActiveMods(modInfos);

        modInfos = modInfos.OrderBy(m => m.Priority == 0).ThenBy(m => m.Priority).ToList();

        foreach (IModInfo modInfo in modInfos) { this.Mods.Add(modInfo); }
        await this.WriteLogBoxLine("Mods loaded.");
    }

    public static void Exit(object? p)
    {
        System.Windows.Application.Current.MainWindow.Close();
    }
    internal async Task ClearLogBox()
    {
        this.LogText = "";
    }

    internal async Task WriteLogBoxLine(string text)
    {
        StringBuilder sb = new(this.LogText);
        if (this.LogText.Length > 0)
        {
            _ = sb.Append(Environment.NewLine);
        }

        _ = sb.Append(text);
        this.LogText = sb.ToString();
    }
    internal async Task WriteLogBox(string text)
    {
        StringBuilder sb = new(this.LogText);
        _ = sb.Append(text);
        this.LogText = sb.ToString();
    }
    private void ReadStartupArguments()
    {
        if (this.startupArguments.Remove("-skyrimDebug64"))
        {
            this.Engine = new BehaviourEngine(new SkyrimDebugConfiguration());
        }
        if (this.startupArguments.Remove("-autoClose"))
        {
            this.closeOnFinish = true;
        }
        foreach (string arg in this.startupArguments)
        {
            if (arg.StartsWith("-o:", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> argArr = arg.AsSpan();
                ReadOnlySpan<char> pathArr = argArr[3..];
                this.Engine.Configuration.Patcher.SetOutputPath(pathArr.Trim().ToString());
                continue;
            }
            //            if (arg.Equals("-sseDebug", StringComparison.OrdinalIgnoreCase))
            //            {
            //                Engine = new BehaviourEngine(new SkyrimDebugConfiguration());
            //                continue;
            //            }
            //            if (arg.Equals("-autorun", StringComparison.OrdinalIgnoreCase))
            //            {
            //	closeOnFinish = true;
            //	launchImmediate = true;
            //                continue;
            //}
        }
        if (this.startupArguments.Remove("-autorun"))
        {
            this.closeOnFinish = true;
            this.autoRun = true;
        }

    }
    private bool LoadActiveMods(List<IModInfo> loadedMods)
    {
        if (!this.activeModConfig.Exists)
        {
            return false;
        }

        foreach (IModInfo mod in loadedMods)
        {
            if (mod == null)
            {
                continue;
            }

            mod.Active = false;
        }
        using FileStream readStream = this.activeModConfig.OpenRead();
        using StreamReader streamReader = new(readStream);
        string? expectedLine;
        uint priority = 0;
        while ((expectedLine = streamReader.ReadLine()) != null)
        {
            if (!this.modsByCode.TryGetValue(expectedLine, out IModInfo? modInfo))
            {
                continue;
            }

            priority++;
            modInfo.Priority = priority;
            modInfo.Active = true;
        }
        return true;
    }
    private void SaveActiveMods(List<IModInfo> activeMods)
    {

        if (this.activeModConfig.Exists) { this.activeModConfig.Delete(); }

        using FileStream writeStream = this.activeModConfig.OpenWrite();
        using StreamWriter streamWriter = new(writeStream);
        foreach (IModInfo modInfo in activeMods)
        {
            streamWriter.WriteLine(modInfo.Code);
        }

    }
    private static List<IModInfo> AssignModPriorities(List<IModInfo> mods)
    {
        uint priority = 0;
        foreach (IModInfo mod in mods)
        {
            priority++;
            mod.Priority = priority;
        }

        return mods;
    }

    private List<IModInfo> GetActiveModsByPriority()
    {
        return AssignModPriorities(this.Mods.Where(m => m.Active).ToList());
    }

    private async void SetEngineConfiguration(object? config)
    {
        if (config == null) { return; }
        IEngineConfigurationFactory engineConfiguration = (IEngineConfigurationFactory)config;
        await this.preloadTask;
        IEngineConfiguration? newConfig = engineConfiguration.Config;
        this.Engine = newConfig != null ? new BehaviourEngine(newConfig) : this.Engine;
    }
    private async void LaunchEngine(object? parameter)
    {
        lock (this.LaunchCommand)
        {
            this.engineRunning = true;
            this.LaunchEnabled = !this.engineRunning;
        }

        this.logText = string.Empty;

        await this.WriteLogBoxLine("Engine launched.");
        await this.preloadTask;
        List<IModInfo> activeMods = this.GetActiveModsByPriority();

        IModInfo? baseModInfo = this.Mods.FirstOrDefault(m => m.Code == "pandora");

        if (baseModInfo == null) { await this.WriteLogBoxLine("FATAL ERROR: Pandora Base does not exist. Ensure the engine was installed properly and data is not corrupted."); return; }
        if (!baseModInfo.Active)
        {
            baseModInfo.Active = true;
            activeMods.Add(baseModInfo);
        }
        baseModInfo.Priority = uint.MaxValue;

        Stopwatch timer = Stopwatch.StartNew();
        bool success = false;
        await Task.Run(async () => { success = await this.Engine.LaunchAsync(activeMods); });

        timer.Stop();

        await this.ClearLogBox();

        await this.WriteLogBoxLine(this.Engine.GetMessages(success));

        if (!success)
        {
            await this.WriteLogBoxLine($"Launch aborted. Existing output was not cleared, and current patch list will not be saved.");
        }
        else
        {
            await this.WriteLogBoxLine($"Launch finished in {Math.Round(timer.ElapsedMilliseconds / 1000.0, 2)} seconds");
            await Task.Run(() => { this.SaveActiveMods(activeMods); });

            if (this.closeOnFinish)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        this.Engine = new BehaviourEngine();
        this.preloadTask = Task.Run(this.Engine.PreloadAsync);

        lock (this.LaunchCommand)
        {
            this.engineRunning = false;
            this.LaunchEnabled = !this.engineRunning;
        }

    }

    private bool CanLaunchEngine(object? parameter)
    {

        return !this.engineRunning;

    }
}
