using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KodiNuke.Utility
{
    public class RegexUtility
    {
        public static string CaseInsensitiveReplace(string sourceStr, string strToReplace, string strReplaceWith)
        {
            return Regex.Replace(sourceStr, Regex.Escape(strToReplace), strReplaceWith, RegexOptions.IgnoreCase);
        }

        public static string CaseInsensitiveReplaceAtStart(string sourceStr, string strToReplace, string strReplaceWith)
        {
            return Regex.Replace(sourceStr, "^" + Regex.Escape(strToReplace), strReplaceWith, RegexOptions.IgnoreCase);
        }
    }
}
