using System;
using System.IO;

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
