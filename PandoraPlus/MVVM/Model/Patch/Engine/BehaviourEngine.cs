using System.Collections.Generic;
using System.Threading.Tasks;
using Pandora.Core.Engine.Configs;

namespace Pandora.Core;

public class BehaviourEngine
{
    public IEngineConfiguration Configuration { get; private set; } = new SkyrimConfiguration();

    public BehaviourEngine()
    {

    }
    public BehaviourEngine(IEngineConfiguration configuration)
    {
        this.Configuration = configuration;
    }
    public void Launch(List<IModInfo> mods)
    {

        this.Configuration.Patcher.SetTarget(mods);
        this.Configuration.Patcher.Update();
        this.Configuration.Patcher.Run();
    }

    public async Task<bool> LaunchAsync(List<IModInfo> mods)
    {
        this.Configuration.Patcher.SetTarget(mods);

        return await this.Configuration.Patcher.UpdateAsync() && await this.Configuration.Patcher.RunAsync();
    }

    public async Task PreloadAsync()
    {
        await this.Configuration.Patcher.PreloadAsync();
    }

    public string GetMessages(bool success)
    {
        return success ? this.Configuration.Patcher.GetPostRunMessages() : this.Configuration.Patcher.GetFailureMessages();
    }
}
