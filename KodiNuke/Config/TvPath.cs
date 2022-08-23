using System.Collections.Generic;

namespace KodiNuke.Config
{
    public class TvPath
    {
        public string RootPath { get; }
        public string Path { get; }
        public IList<string> NetworkPaths { get; }

        public TvPath(string rootPath, string path, params string[] networkPaths)
        {
            RootPath = rootPath;
            Path = path;
            NetworkPaths = networkPaths;
        }
    }
}