using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Media_DL.Discord
{
   
    public static class Channel
    {
        public enum AttachmentType
        {
            Image = 0,
            Video = 1
        }

        public struct Attachment
        {
            public AttachmentType Type { get; set; }
            public string Url { get; set; }
        }

        public static async Task<bool> IsValid(string channelID, string token)
        {
            if (string.IsNullOrEmpty(channelID))
                return false;
            
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("authorization", token);

                // if this request goes through it has to be valid :)
                await client.DownloadStringTaskAsync($"https://discord.com/api/v9/channels/{channelID}/messages?limit=1");

                client.Dispose();

                return true;
            }
            catch(Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif 
            }

            return false;
        }
    }
}
