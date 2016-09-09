using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarrSharp
{
    public class SonarrSeries
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("tvdbId")] public int? TvdbId { get; set; }
        [JsonProperty("tvRageId")] public int? TvRageId { get; set; }
        [JsonProperty("tvMazeId")] public int? TvMazeId { get; set; }
        [JsonProperty("imdbId")] public string ImdbId { get; set; }

        [JsonProperty("sizeOnDisk")] public long SizeOnDisk { get; set; }

        [JsonProperty("images")] public List<Image> Images { get; set; }

        public string ImageBaseUrl { get; set; }

        public override string ToString()
            => Title;

        public class Image
        {
            [JsonProperty("coverType")] public string CoverType { get; set; }
            [JsonProperty("url")] public string Url { get; set; }
        }
    }
}
