using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Media_DL.Discord
{
    public delegate void ChannelReaderProgressUpdateHandler(double percent);
    public delegate void ChannelReaderErrorHandler(Exception error);

    public class ChannelReader
    {
        public event ChannelReaderProgressUpdateHandler OnProgressUpdated;
        public event ChannelReaderErrorHandler OnError;

        public string ChannelID { get; private set; }
        public string Token { get; private set; }

        public ChannelReader(string channelID, string token)
        {
            ChannelID = channelID;
            Token = token;
        }

        public enum IndexMode
        {
            IndexAll,
            IndexImages,
            IndexVideos
        }

        public async Task<string[]> IndexAttachments(int max = 1000, IndexMode mode = IndexMode.IndexAll)
        {
            List<string> result = new List<string>();

            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("authorization", Token);

                int counter = 0;

                string data = await client.DownloadStringTaskAsync($"https://discord.com/api/v9/channels/{ChannelID}/messages?limit={Math.Min(max - counter, 50)}");
                JArray messages = (JArray)JsonConvert.DeserializeObject(data);

                string lastMessageID = "";

                Action parseMessages = () =>
                {
                    foreach (JObject j in messages)
                    {
                        if (j.TryGetValue("attachments", out JToken jt))
                        {
                            foreach (JObject media in (JArray)jt)
                            {
                                if (media.TryGetValue("content_type", out JToken ct))
                                {
                                    string contentType = ct.ToString();

                                    if (contentType.StartsWith("image"))
                                    {
                                        if (mode == IndexMode.IndexAll || mode == IndexMode.IndexImages)
                                            if (media.TryGetValue("url", out JToken jurl))
                                            {
                                                if (result.Contains(jurl.ToString()) == false)
                                                    result.Add(jurl.ToString());
                                            }
                                    }
                                    else
                                    if (contentType.StartsWith("video"))
                                    {
                                        if (mode == IndexMode.IndexAll || mode == IndexMode.IndexVideos)
                                            if (media.TryGetValue("url", out JToken jurl))
                                            {
                                                if (result.Contains(jurl.ToString()) == false)
                                                    result.Add(jurl.ToString());
                                            }
                                    }
                                }
                            }
                        }

                        if (j.TryGetValue("id", out JToken mjt))
                        {
                            lastMessageID = mjt.ToString();
                        }
                    }
                };

                parseMessages();

                counter += messages.Count;

                // only enter this scope if there is *actually* more messages
                if (messages.Count >= 50)
                {
                    OnProgressUpdated?.Invoke((100.0 / max) * counter);

                    while (true)
                    {
                        try
                        {
                            int limit = Math.Min(max - counter, 50);

                            if (limit == 0) // sending a limit of 0 will result in an error from the api
                                break;

                            data = await client.DownloadStringTaskAsync($"https://discord.com/api/v9/channels/{ChannelID}/messages?before={lastMessageID}&limit={limit}");
                            messages = (JArray)JsonConvert.DeserializeObject(data);

                            counter += messages.Count;

                            OnProgressUpdated?.Invoke((100.0 / max) * counter);

                            parseMessages();

                            if (messages.Count != 50) // if it doesnt return 50, we have reached the end of the chat.
                                break;
                        }
                        catch { }
                    }
                }
                else
                    OnProgressUpdated?.Invoke((100.0 / messages.Count) * counter);


                client.Dispose();
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif 
                OnError?.Invoke(e);
            }

            return result.ToArray();
        }
    }

}
