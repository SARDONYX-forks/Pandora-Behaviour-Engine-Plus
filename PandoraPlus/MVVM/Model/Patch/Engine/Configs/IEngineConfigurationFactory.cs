namespace Pandora.Core.Engine.Configs;

public interface IEngineConfigurationFactory
{
    public string Name { get; }
    public IEngineConfiguration? Config { get; }
    public bool Selectable => this.Config != null;
}
