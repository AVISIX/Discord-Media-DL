using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Media_DL.Misc
{
    public static class Extensions
    {
        public static string GetFilenameFromUrl(this string url)
        {
            return Path.GetFileName(new Uri(url).LocalPath);
        }
    }
}
