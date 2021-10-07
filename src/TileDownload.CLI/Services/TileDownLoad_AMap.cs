using System.Net.Http;

using TileDownload.CLI.Utils;

namespace TileDownload.CLI.Services
{
    internal class TileDownLoad_AMap : TileDownLoad_Default
    {
        public TileDownLoad_AMap(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
        {
        }

        public override (int x, int y) LatLng2TileNumber(double lng, double lat, int zoom)
        {
            var latlng = CoordinateHelper.WGS84_To_GCJ02(lat, lng);

            return base.LatLng2TileNumber(latlng[1], latlng[0], zoom);
        }
    }
}
