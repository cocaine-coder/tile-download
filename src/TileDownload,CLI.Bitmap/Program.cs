using Microsoft.Extensions.DependencyInjection;
using System;
using TileDownload.CLI.Services;

namespace TileDownload_CLI.Bitmap
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new TileDownLoadConfig();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient();

            serviceCollection.AddTransient<ITileDownLoad, TileDownLoad_AMap>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.GetRequiredService<ITileDownLoad>().Run(config);
        }
    }
}
