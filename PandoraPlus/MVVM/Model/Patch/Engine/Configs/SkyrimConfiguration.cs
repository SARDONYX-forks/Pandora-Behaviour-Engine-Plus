using Pandora.Core.Patchers;
using Pandora.Patch.IOManagers.Skyrim;
using Pandora.Patch.Patchers.Skyrim;

namespace Pandora.Core.Engine.Configs;

public class SkyrimConfiguration : IEngineConfiguration
{
    public string Name { get; } = "Skyrim SE/AE";

    public string Description { get; } =
    @"Engine configuration for Skyrim SE/AE behavior files";

    public IPatcher Patcher { get; } = new SkyrimPatcher(new PackFileExporter());

}
