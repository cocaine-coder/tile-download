using System.Threading.Tasks;

namespace TileDownload.CLI.Services
{
    public interface ITileDownLoad
    {
        void Run(TileDownLoadConfig tileConfig);
    }
}
