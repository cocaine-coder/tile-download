using System;
using System.Text.Json.Serialization;

namespace TileDownload.CLI.Services
{
    public class TileDownLoadConfig
    {
        private int zoom;
        private int maxcup;

        public TileDownLoadConfig()
        {
            Zoom = 18;
            MapUrlTemplate = "http://webst03.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={z}";
            LeftTopPoint = new Point(116.385313, 39.921463);
            RightBottomPoint = new Point(116.39628, 39.91186);

            OutputDir = AppDomain.CurrentDomain.BaseDirectory;
            MaxCPU = 4;
            IsSaveTiles = false;
        }

        /// <summary>
        /// 地图服务模板url
        /// </summary>
        [JsonIgnore]
        public string MapUrlTemplate { get; set; }

        /// <summary>
        /// 下载层级
        /// </summary>
        public int Zoom
        {
            get
            {
                if (zoom < 1)
                    zoom = 1;

                if (zoom > 18)
                    zoom = 18;

                return zoom;
            }
            set
            {
                zoom = value;
            }
        }

        /// <summary>
        /// 左上角经纬度
        /// </summary>
        public Point LeftTopPoint { get; set; }

        /// <summary>
        /// 右下角经纬度
        /// </summary>
        public Point RightBottomPoint { get; set; }

        /// <summary>
        /// 输入目录
        /// </summary>
        public string OutputDir { get; set; }

        /// <summary>
        /// 最大核心
        /// </summary>
        public int MaxCPU
        {
            get
            {
                if(maxcup < 1) maxcup = 1;
                return maxcup;
            }
            set
            {
                maxcup = value;
            }
        }

        /// <summary>
        /// 是否存储瓦片
        /// </summary>
        public bool IsSaveTiles { get; set; }

        public override string ToString()
        {
            return $"map_url_template : {MapUrlTemplate}\n" +
                $"zoom : {Zoom}\n" +
                $"box : {LeftTopPoint} {RightBottomPoint}\n" +
                $"output : {OutputDir}\n" +
                $"max_cpu : {MaxCPU}\n" +
                $"is_save_tiles : {IsSaveTiles}";
        }
    }

    public class Point
    {
        public Point(double lng, double lat)
        {
            Lng = lng;
            Lat = lat;
        }
        public double Lng { get; set; }
        public double Lat { get; set; }

        public override string ToString()
        {
            return $"{Lng},{Lat}";
        }
    }
}
