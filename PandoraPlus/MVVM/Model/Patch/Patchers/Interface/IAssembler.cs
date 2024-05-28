using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pandora.Core;

namespace Pandora.Patch.Patchers;

public interface IAssembler
{
    public void LoadResources();

    public Task LoadResourcesAsync();

    public void AssemblePatch(IModInfo mod);

    public void GetPostMessages(StringBuilder builder);

    public virtual async Task AssemblePatchAsync(IModInfo mod)
    {
        await Task.Run(() => this.AssemblePatch(mod));
    }
    public bool ApplyPatches();

    public Task<bool> ApplyPatchesAsync();

    public List<(FileInfo inFile, FileInfo outFile)> GetExportFiles();

}
