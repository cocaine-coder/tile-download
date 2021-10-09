using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using MaxRev.Gdal.Core;
using OSGeo.GDAL;

using TileDownload.CLI.Utils;

namespace TileDownload.CLI.Services
{
    internal class TileDownLoad_Default : ITileDownLoad
    {
        private readonly IHttpClientFactory httpClientFactory;

        private IProgress<ProgressReporter> progress1= null;
        private IProgress<ProgressReporter> progress2 = null;

        public TileDownLoad_Default(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
            GdalBase.ConfigureAll();
        }

        public void Run(TileDownLoadConfig tileConfig, params IProgress<ProgressReporter>[] progresses)
        {
            if(progresses != null)
            {
                if(progresses.Any()) progress1 = progresses[0];
                if(progresses.Length > 1) progress2 = progresses[1];
            }
             
            var destDir = tileConfig.OutputDir;
            var (xMin, yMin) = LatLng2TileNumber(tileConfig.LeftTopPoint.Lng, tileConfig.LeftTopPoint.Lat, tileConfig.Zoom);
            var (xMax, yMax) = LatLng2TileNumber(tileConfig.RightBottomPoint.Lng, tileConfig.RightBottomPoint.Lat, tileConfig.Zoom);

            var driver = Gdal.GetDriverByName("GTiff");
            var destDataset = driver.Create(
                Path.Combine(destDir, $"{tileConfig.Zoom}-{tileConfig.LeftTopPoint},{tileConfig.RightBottomPoint}.tif"),
                256 * (xMax - xMin + 1),
                256 * (yMax - yMin + 1),
                3, DataType.GDT_Byte, null);

            int count = 0;
            int total = (xMax - xMin + 1) * (yMax - yMin + 1);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = tileConfig.MaxCPU };
            Parallel.For(xMin, xMax + 1, parallelOptions, x =>
            {
                var tileDir = Path.Combine(destDir, tileConfig.Zoom.ToString(), x.ToString());
                if (!Directory.Exists(tileDir))
                    Directory.CreateDirectory(tileDir);

                Parallel.For(yMin, yMax + 1, parallelOptions, y =>
                {
                    var srcFile = Path.Combine(tileDir, $"{y}.png");
                    var buffer = httpClientFactory.CreateClient()
                      .GetByteArrayAsync(CreateTileUrl(tileConfig.MapUrlTemplate, x, y, tileConfig.Zoom)).Result;
                    File.WriteAllBytes(srcFile, buffer);

                    count += 1;
                    progress1?.Report(new ProgressReporter { Count = count, Total = total, Message = "下载进度" });
                });
            });

            count = 0;
            for (int x = xMin; x < xMax +1; x++)
            {
                for (int y = yMin; y < yMax + 1; y++)
                {
                    var srcFile = Path.Combine(destDir, tileConfig.Zoom.ToString(), x.ToString(), $"{y}.png");
                    using var srcDataset = Gdal.Open(srcFile, Access.GA_ReadOnly);
                    SaveBuffered(srcDataset, destDataset, x - xMin, y - yMin);
                    count += 1;
                    progress2?.Report(new ProgressReporter { Count = count,Total = total,Message = "拼接进度" });
                }
            }

            destDataset.FlushCache();
            destDataset.Dispose();

            if (!tileConfig.IsSaveTiles)
            {
                Directory.Delete(Path.Combine(destDir, tileConfig.Zoom.ToString()), true);
            }
        }

        /// <summary>
        /// 经纬度转化为瓦片编号
        /// </summary>
        /// <param name="lng">经度(角度)</param>
        /// <param name="lat">纬度(角度)</param>
        /// <returns></returns>
        public virtual (int x, int y) LatLng2TileNumber(double lng, double lat, int zoom)
        {
            var n = Math.Pow(2, zoom);
            var tileX = (lng + 180) / 360 * n;
            var tileY = (1 - (Math.Log(Math.Tan(Degree2Radians(lat)) + (1 / Math.Cos(Degree2Radians(lat)))) / Math.PI)) / 2 * n;

            return ((int)Math.Floor(tileX), (int)Math.Floor(tileY));
        }

        /// <summary>
        /// 瓦片编号转化为经纬度
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public virtual (double lng, double lat) TileNumber2LatLng(int x, int y, int zoom)
        {
            var n = Math.Pow(2, zoom);
            var lng = x / n * 360.0 - 180.0;
            var lat = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));

            return (lng, Radians2Degree(lat));
        }

        /// <summary>
        /// 创建地图瓦片url
        /// </summary>
        /// <param name="urlTemplate">url模板</param>
        /// <param name="x">瓦片编号 x</param>
        /// <param name="y">瓦片编号 y</param>
        /// <param name="zoom">缩放级别</param>
        /// <returns></returns>
        public virtual string CreateTileUrl(string urlTemplate, int x, int y, int zoom) =>
            urlTemplate.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", zoom.ToString());

        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="degree">角度</param>
        /// <returns></returns>
        protected double Degree2Radians(double degree)
        {
            return degree * Math.PI / 180;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="radians">弧度</param>
        /// <returns></returns>
        protected double Radians2Degree(double radians)
        {
            return radians * 180 / Math.PI;
        }

        /// <summary>
        /// GTiff 存储
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void SaveBuffered(Dataset src, Dataset dst, int x, int y)
        {
            var redBand = src.GetRasterBand(1);
            var greenBand = src.GetRasterBand(2);
            var blueBand = src.GetRasterBand(3);

            var width = redBand.XSize;
            var height = redBand.YSize;

            var r = new byte[width * height];
            var g = new byte[width * height];
            var b = new byte[width * height];

            redBand.ReadRaster(0, 0, width, height, r, width, height, 0, 0);
            greenBand.ReadRaster(0, 0, width, height, g, width, height, 0, 0);
            blueBand.ReadRaster(0, 0, width, height, b, width, height, 0, 0);

            dst.GetRasterBand(1).WriteRaster(x * width, y * height, width, height, r, width, height, 0, 0);
            dst.GetRasterBand(2).WriteRaster(x * width, y * height, width, height, g, width, height, 0, 0);
            dst.GetRasterBand(3).WriteRaster(x * width, y * height, width, height, b, width, height, 0, 0);
        }
    }
}
