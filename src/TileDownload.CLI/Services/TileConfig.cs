using System.Text.Json.Serialization;

namespace TileDownload.CLI.Services
{
    public class TileConfig
    {
        public TileConfig()
        {
            Zoom = 17;
            MapUrlTemplate = "https://maponline1.bdimg.com/starpic/?qt=satepc&u=x={x};y={y};z={z};v=009;type=sate&fm=46&udt=20210927";
            LeftTopPoint = new Point(0, 0);
            RightBottomPoint = new Point(0, 0);
        }

        [JsonIgnore]
        public string MapUrlTemplate { get; set; }

        public int Zoom { get; set; }

        public Point LeftTopPoint { get; set; }

        public Point RightBottomPoint { get; set; }

        public override string ToString()
        {
            return $"mapurl : {MapUrlTemplate}\nzoom : {Zoom}\nbox:{LeftTopPoint.Lng},{LeftTopPoint.Lat} {RightBottomPoint.Lng},{RightBottomPoint.Lat}";
        }
    }

    public class Point
    {
        public Point(double lng,double lat)
        {
            Lng = lng;
            Lat = lat;
        }
        public double Lng { get; set; }
        public double Lat { get; set; }
    }
}
