using Pandora.Core.Patchers;

namespace Pandora.Core;

public interface IEngineConfiguration
{
    string Name { get; }

    string Description { get; }

    public IPatcher Patcher { get; }

}
