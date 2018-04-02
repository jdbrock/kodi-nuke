using KodiSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KodiNuke
{
    public class Movie
    {
        public KodiMovie Kodi { get; set; }

        public override string ToString()
            => Kodi.Title;
    }
}
