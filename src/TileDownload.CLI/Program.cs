using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

using TileDownload.CLI.Services;
using TileDownload.CLI.Utils;

namespace TileDownload.CLI
{
    internal class Program
    {
        const string helpContent = "commond params :\n   --getconfig | -gc : 获取配置文件\n   --config | -c <path> : 设置配置文件并运行";
        private static TileDownLoadConfig tileConfig = new();
        private static readonly object _lock = new();

        static void Main(string[] args)
        {
            SetArgs(args);
            LogHelper.LogHeader("配置参数");
            LogHelper.LogInfo(tileConfig.ToString());
            LogHelper.LogInfo("");

            LogHelper.LogHeader("开始运行");
            var (consoleLeft, consoleTop) = Console.GetCursorPosition();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            serviceCollection.AddTransient<ITileDownLoad, TileDownLoad_AMap>();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var tileDownload = serviceProvider.GetRequiredService<ITileDownLoad>();

            IProgress<ProgressReporter> downloadProgress = new Progress<ProgressReporter>(value =>
            {
                lock (_lock)
                {
                    Console.SetCursorPosition(consoleLeft, consoleTop);
                    Console.Write(value);
                }
            });

            IProgress<ProgressReporter> mergeProgress = new Progress<ProgressReporter>(value =>
            {
                lock (_lock)
                {
                    Console.SetCursorPosition(consoleLeft, consoleTop + 1);
                    Console.Write(value);
                }
            });

            var stopwatch = Stopwatch.StartNew();
            tileDownload.Run(tileConfig, downloadProgress, mergeProgress);
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"耗时 : {stopwatch.ElapsedMilliseconds / 1000.0} s");
        }

        private static void SetArgs(string[] args)
        {
            if (args.Any())
            {
                var firstFlag = args.First().Trim();

                //帮助文档
                if (firstFlag == "--help" || firstFlag == "-h")
                    LogHelper.Exit(helpContent);


                //生成默认配置文件,文件放在程序根目录
                else if (firstFlag == "--getconfig" || firstFlag == "-gc")
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json"), JsonSerializer.Serialize(tileConfig));
                    Environment.Exit(0);
                }

                //读取配置文件并运行
                else if (firstFlag == "--config" || firstFlag == "-c")
                {
                    //参数少了
                    if (args.Length < 2)
                        LogHelper.Exit("需要提供配置文件路径");

                    var configPath = args[1];
                    if (!File.Exists(configPath))
                        LogHelper.Exit("配置文件不存在，请检查后重试");

                    try
                    {
                        tileConfig = JsonSerializer.Deserialize<TileDownLoadConfig>(File.ReadAllText(configPath));

                        //校验文件路径
                        if (!Directory.Exists(tileConfig.OutputDir))
                            LogHelper.Exit("输出文件夹不存在");
                    }
                    catch (Exception)
                    {
                        LogHelper.Exit("配置文件格式出错,请检查后重试");
                    }
                }
                else
                {
                    LogHelper.Exit(helpContent);
                }
            }
            else
            {
                LogHelper.Exit(helpContent);
            }
        }
    }
}
