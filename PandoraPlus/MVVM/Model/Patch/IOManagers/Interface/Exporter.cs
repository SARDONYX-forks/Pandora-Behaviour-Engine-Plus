using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pandora.Patch.IOManagers;
public interface Exporter<T>
{
    public DirectoryInfo ExportDirectory { get; set; }
    public bool Export(T obj);
    public T Import(FileInfo file);

    public bool ExportParallel(IEnumerable<T> objs)
    {
        bool success = true;
        _ = Parallel.ForEach(objs, obj => { if (!this.Export(obj)) { success = false; } });
        return success;
    }
}
