using Humanizer.Bytes;
using KodiSharp;
using SonarrSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using PropertyChanged;

namespace KodiNuke
{
    [ImplementPropertyChanged]
    public class Series
    {
#if !DISABLE_KODI
        public KodiTvShow Kodi { get; set; }
#endif

        public SonarrSeries Sonarr { get; set; }

        [AlsoNotifyFor(nameof(Sonarr))]
        public string PosterImageUri
        {
            get
            {
                var path = Sonarr?.Images?.FirstOrDefault(x => x.CoverType == "poster")?.Url
                    ?? Sonarr?.Images?.FirstOrDefault()?.Url;

                if (String.IsNullOrWhiteSpace(path))
                    return null;

                path = path.Replace("/sonarr/", "");

                if (Sonarr.ImageBaseUrl == null || path == null)
                    return null;

                return new Uri(new Uri(Sonarr.ImageBaseUrl), path).OriginalString;
            }
        }

        [AlsoNotifyFor(nameof(Sonarr))]
        public string BackgroundImageUri
        {
            get
            {
                var path = Sonarr?.Images?.FirstOrDefault(x => x.CoverType == "fanart")?.Url;

                if (String.IsNullOrWhiteSpace(path))
                    return null;

                path = path.Replace("/sonarr/", "");

                if (Sonarr.ImageBaseUrl == null || path == null)
                    return null;

                return new Uri(new Uri(Sonarr.ImageBaseUrl), path).OriginalString;
            }
        }

        public string HumanSize => Sonarr.SizeOnDisk == 0
            ? "0"
            : ByteSize.FromBytes(Sonarr.SizeOnDisk).Humanize("#");

#if !DISABLE_KODI
        public Series(KodiTvShow kodi, SonarrSeries sonarr)
        {
            Kodi = kodi;
            Sonarr = sonarr;
        }
#endif

        public Series(SonarrSeries sonarr)
        {
            Sonarr = sonarr;
        }

        public override string ToString()
            => $"{Sonarr.Title} ({HumanSize})";
    }
}
