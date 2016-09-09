using Humanizer.Bytes;
using KodiSharp;
using SonarrSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;

namespace KodiNuke
{
    public class Series
    {
        public KodiTvShow Kodi { get; set; }
        public SonarrSeries Sonarr { get; set; }

        public string PosterImageUri
        {
            get
            {
                var path = Sonarr?.Images?.FirstOrDefault(x => x.CoverType == "poster")?.Url
                    ?? Sonarr?.Images?.FirstOrDefault()?.Url;

                if (String.IsNullOrWhiteSpace(path))
                    return null;

                path = path.Replace("/sonarr/", "");

                return new Uri(new Uri(Sonarr.ImageBaseUrl), path).OriginalString;
            }
        }

        public string BackgroundImageUri
        {
            get
            {
                var path = Sonarr?.Images?.FirstOrDefault(x => x.CoverType == "fanart")?.Url;

                if (String.IsNullOrWhiteSpace(path))
                    return null;

                path = path.Replace("/sonarr/", "");

                return new Uri(new Uri(Sonarr.ImageBaseUrl), path).OriginalString;
            }
        }

        public string HumanSize => ByteSize.FromBytes(Sonarr.SizeOnDisk).Humanize("#");

        public Series(KodiTvShow kodi, SonarrSeries sonarr)
        {
            Kodi = kodi;
            Sonarr = sonarr;
        }

        public override string ToString()
            => Sonarr.Title;
    }
}
