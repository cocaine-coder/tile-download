using System;

namespace TileDownload.CLI.Services
{
    public interface ITileDownLoad
    {
        void Run(TileDownLoadConfig tileConfig, IProgress<string> progress = null);
    }
}
