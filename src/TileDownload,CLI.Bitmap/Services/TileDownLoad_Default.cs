using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TileDownload.CLI.Services
{
    public class TileDownLoad_Default : ITileDownLoad
    {
        private readonly static object _lock = new object();
        private readonly IHttpClientFactory httpClientFactory;

        public TileDownLoad_Default(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public void Run(TileDownLoadConfig tileConfig)
        {
            var destDir = tileConfig.OutputDir;
            var (xMin, yMin) = LatLng2TileNumber(tileConfig.LeftTopPoint.Lng, tileConfig.LeftTopPoint.Lat, tileConfig.Zoom);
            var (xMax, yMax) = LatLng2TileNumber(tileConfig.RightBottomPoint.Lng, tileConfig.RightBottomPoint.Lat, tileConfig.Zoom);

            var imageWidth = 256 * (xMax - xMin + 1);
            var imageHeight = 256 * (yMax - yMin + 1);

            var destBitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format24bppRgb);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = tileConfig.MaxCPU };
            Parallel.For(xMin, xMax + 1, parallelOptions, x =>
            {
                Parallel.For(yMin, yMax + 1, parallelOptions, y =>
                {
                    var stream = httpClientFactory.CreateClient()
                      .GetStreamAsync(CreateTileUrl(tileConfig.MapUrlTemplate, x, y, tileConfig.Zoom)).Result;

                    lock (_lock)
                    {
                        var srcBitmap = new Bitmap(stream);

                        var destBitmapData = destBitmap.LockBits(
                            new Rectangle((x - xMin) * 256, (y - yMin) * 256, 256, 256),
                            ImageLockMode.ReadWrite,
                            destBitmap.PixelFormat);

                        var srcBitmapData = srcBitmap.LockBits(
                            new Rectangle(0, 0, 256, 256),
                            ImageLockMode.ReadOnly,
                            srcBitmap.PixelFormat);

                        SaveBuffered(srcBitmapData, destBitmapData);

                        destBitmap.UnlockBits(destBitmapData);
                        srcBitmap.UnlockBits(srcBitmapData);
                    }
                });
            });

            destBitmap.Save(Path.Combine(tileConfig.OutputDir, "result.tif"));

            stopwatch.Stop();
            LogHelper.LogInfo($"拼接完成 ：{stopwatch.ElapsedMilliseconds}");
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
        private void SaveBuffered(BitmapData src, BitmapData dest)
        {
            unsafe
            {
                byte* srcP = (byte*)src.Scan0;
                byte* destP = (byte*)dest.Scan0;

                for (int i = 0; i < src.Height; i++)
                {
                    for (int j = 0; j < src.Width; j++)
                    {
                        destP[0] = srcP[0];
                        destP[1] = srcP[1];
                        destP[2] = srcP[2];

                        destP += 3;
                        srcP += 3;
                    }

                    srcP += src.Stride - src.Width * 3;
                    destP += dest.Stride - dest.Width * 3;
                }
            }
        }
    }
}
