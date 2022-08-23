using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KodiNuke.Models
{
    public class PathMap
    {
        public string From { get; set; }
        public string To { get; set; }

        public PathMap(string from, string to)
        {
            From = from;
            To = to;
        }
    }
}
