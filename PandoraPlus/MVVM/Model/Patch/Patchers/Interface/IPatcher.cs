﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pandora.Core.Patchers;

public interface IPatcher
{
    [Flags]
    public enum PatcherFlags
    {
        None = 0,
        PreloadFailed = 1 << 1,
        UpdateFailed = 1 << 2,
        LaunchFailed = 1 << 3,
        Success = ~PreloadFailed & ~UpdateFailed & ~LaunchFailed
    }
    public PatcherFlags Flags { get; }

    public string GetVersionString();

    public Version GetVersion();
    public void SetTarget(List<IModInfo> mods);

    public Task PreloadAsync();

    public void Update();

    public string GetPostUpdateMessages()
    {
        return string.Empty;
    }

    public void Run();

    public string GetPostRunMessages()
    {
        return string.Empty;
    }

    public string GetFailureMessages();

    public Task<bool> UpdateAsync();

    public Task<bool> RunAsync();

    public void SetOutputPath(DirectoryInfo directoryInfo);

    public void SetOutputPath(string outputPath)
    {
        this.SetOutputPath(new DirectoryInfo(outputPath));
    }
}
