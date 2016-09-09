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
