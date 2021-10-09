using System;

using TileDownload.CLI.Utils;

namespace TileDownload.CLI.Services
{
    internal interface ITileDownLoad
    {
        void Run(TileDownLoadConfig tileConfig, params IProgress<ProgressReporter>[] progresses);
    }
}
