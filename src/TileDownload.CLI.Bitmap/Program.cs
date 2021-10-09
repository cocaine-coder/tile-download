using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using TileDownload.CLI.Services;

namespace TileDownload.CLI.Minify
{
    internal class Program
    {
        static object _lock = new();
        static TileDownLoadConfig config = new();

        static readonly string helpContent = "commond params :\n   --getconfig | -gc : 获取配置文件\n   --config | -c <path> : 设置配置文件并运行";

        static void Main(string[] args)
        {
            SetConfigByArgs(args);

            LogHelper.LogHeader("配置清单");
            LogHelper.LogInfo(config.ToString());
            LogHelper.LogInfo();

            LogHelper.LogHeader("开始运行");
            Console.Write("进度 : ");
            var (consoleLeft,consoleTop) = Console.GetCursorPosition();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient();
            serviceCollection.AddTransient<ITileDownLoad, TileDownLoad_AMap>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();

            IProgress<string> progress = new Progress<string>(value =>
            {
                lock (_lock)
                {
                    Console.SetCursorPosition(consoleLeft, consoleTop); Console.Write(value);
                }
            });

            var stopWatch = Stopwatch.StartNew();

            serviceProvider.GetRequiredService<ITileDownLoad>().Run(config, progress);

            stopWatch.Stop();
            LogHelper.LogInfo("");
            LogHelper.LogInfo($"运行结束，共用时 : {stopWatch.ElapsedMilliseconds / 1000.0} s");
        }

        static void SetConfigByArgs(string[] args)
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
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json"), JsonSerializer.Serialize(config));
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
                        config = JsonSerializer.Deserialize<TileDownLoadConfig>(File.ReadAllText(configPath));

                        //校验文件路径
                        if (!Directory.Exists(config.OutputDir))
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
