using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using TileDownload.CLI.Services;

namespace TileDownload.CLI
{
    internal class Program
    {
        private static string destDir;
        private static TileConfig tileConfig = new();

        static async Task Main(string[] args)
        {
            LogHelper.LogHeader("配置参数");
            SetArgs(args);
            LogHelper.LogInfo(tileConfig.ToString());
            LogHelper.LogInfo("");

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient();
            serviceCollection.AddTransient<ITileDownLoad, TileDownLoad_BdMap>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();

            var tileDownload = serviceProvider.GetRequiredService<ITileDownLoad>();

            LogHelper.LogHeader("开始下载");
            await tileDownload.RunAsync(destDir, tileConfig);
            LogHelper.LogInfo("");

            LogHelper.LogHeader("下载完成");
        }

        private static void SetArgs(string[] args)
        {
            if (args.Length < 1)
            {
                LogHelper.Exit("请提供输出文件夹", "TileDownload.CLI.exe <dest_dir> <box> <zoom:可选(默认=17)>");
            }

            destDir = args[0];
            if (!Directory.Exists(destDir))
            {
                LogHelper.Exit("文件夹不存在");
            }

            if (args.Length < 2)
            {
                LogHelper.Exit("请提供范围(box)参数", "参数格式：x1,y1,x2,y2", "x1：左上角经度  y1：左上角纬度  x2：右下角经度  y2：右下角纬度");
            }

            double[] box = null;
            try
            {
                box = args[1].Split(',').Select(x => double.Parse(x.Trim())).ToArray();
            }
            catch (Exception)
            {
                LogHelper.Exit("范围(box)参数转化失败");
            }

            if (box.Count() != 4)
            {
                LogHelper.Exit("范围(box)参数数量必须为 4");
            }

            tileConfig.LeftTopPoint =  new Point(box[0], box[1]);
            tileConfig.RightBottomPoint = new Point(box[2], box[3]);

            if (args.Length > 2)
            {
                bool ret = int.TryParse(args[2], out int zoom);
                if (!ret || zoom<1 || zoom > 23)
                {
                    LogHelper.Exit("zoom参数转化失败，必须为整数。且 0 < zoom < 24");
                }

                tileConfig.Zoom = zoom;
            }
        }
    }
}
