using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

using TileDownload.CLI.Services;

namespace TileDownload.CLI
{
    internal class Program
    {
        private static readonly string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json");
        private static TileDownLoadConfig tileConfig = new();

        static void Main(string[] args)
        {
            LogHelper.LogHeader("配置参数");
           
            if (!File.Exists(configFile))
            {
                File.WriteAllText(configFile,JsonSerializer.Serialize(tileConfig));
                LogHelper.Exit($"未发现配置文件，已重载配置，{configFile}");
            }

            try
            {
                tileConfig = JsonSerializer.Deserialize<TileDownLoadConfig>(File.ReadAllText(configFile));
            }
            catch (Exception)
            {
                LogHelper.Exit($"配置文件读取失败，请检查格式(json)，{configFile}");
            }

            LogHelper.LogInfo(tileConfig.ToString());
            LogHelper.LogInfo("");

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient();
            serviceCollection.AddTransient<ITileDownLoad, TileDownLoad_AMap>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();

            var tileDownload = serviceProvider.GetRequiredService<ITileDownLoad>();

            LogHelper.LogHeader("处理中");
            tileDownload.Run(tileConfig);
            LogHelper.LogInfo("");
        }

        private static void SetArgs(string[] args)
        {
            if (args.Length < 1)
            {
                LogHelper.Exit("请提供输出文件夹", "TileDownload.CLI.exe <dest_dir> <box> <zoom:可选(默认=17)>");
            }

            tileConfig.OutputDir = args[0];
            if (!Directory.Exists(tileConfig.OutputDir))
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
                if (!ret || zoom<1 || zoom > 18)
                {
                    LogHelper.Exit("zoom参数转化失败，必须为整数。且 0 < zoom < 18");
                }

                tileConfig.Zoom = zoom;
            }
        }
    }
}
