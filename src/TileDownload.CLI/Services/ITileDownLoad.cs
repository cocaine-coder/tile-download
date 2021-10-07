using System.Threading.Tasks;

namespace TileDownload.CLI.Services
{
    public interface ITileDownLoad
    {
        Task RunAsync(string destPath, TileConfig tileConfig);
    }
}
