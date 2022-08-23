using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KodiNuke.Config
{
    public class Configuration
    {
        public IList<TvPath> TvPaths { get; set; }

        public Configuration()
        {
            TvPaths = new List<TvPath>();
        }
    }
}
