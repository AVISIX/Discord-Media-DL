using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Media_DL.Discord
{
    public delegate void ChannelReaderProgressUpdateHandler(double percent);
    public delegate void ChannelReaderErrorHandler(Exception error);

    public enum AttachmentType
    {
        Image = 0,
        Video = 1,
        Text = 2
    }

    public struct Author
    {
        public string Name { get; set; }

        public string Discriminator { get; set; }
    }

    public struct Attachment
    {
        public AttachmentType Type { get; set; }

        public string Content { get; set; }

        public string TimeStamp { get; set; }

        public Author Author { get; set; }

        public Attachment(AttachmentType type, string content, string timeStamp, Author author)
        {
            TimeStamp = timeStamp;
            Content = content;
            Type = type;
            Author = author;
        }
    }

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
            IndexVideos,
            IndexVideosAndImages,
            IndexText
        }

        public async Task<Attachment[]> IndexAttachments(int max = 1000, IndexMode mode = IndexMode.IndexAll)
        {
            List<Attachment> result = new();
            List<string> alreadyIndexed = new();

            try
            {
                WebClient client = new();
                client.Headers.Add("authorization", Token);

                var counter = 0;

                var data = await client.DownloadStringTaskAsync($"https://discord.com/api/v9/channels/{ChannelID}/messages?limit={Math.Min(max - counter, 50)}");
                JArray messages = (JArray)JsonConvert.DeserializeObject(data);

                var lastMessageID = "";

                Action parseMessages = () =>
                {
                    foreach (JObject j in messages)
                    {
                        try
                        {
                            if (j.ContainsKey("author") == false || j.ContainsKey("timestamp") == false)
                                continue;

                            var timeStamp = j.GetValue("timestamp").ToString();

                            try
                            {
                                DateTime time = DateTime.Parse(timeStamp);
                                timeStamp = $"{time.Day}/{time.Month}/{time.Year}";
                            }
                            catch { }

                            JObject jAuthor = (JObject)j.GetValue("author");

                            Author author = new()
                            {
                                Name = jAuthor.GetValue("username").ToString(),
                                Discriminator = jAuthor.GetValue("discriminator").ToString()
                            };

                            //    Console.WriteLine(JsonConvert.SerializeObject(j, Formatting.Indented));

                            if (j.TryGetValue("attachments", out JToken jt))
                            {
                                foreach (JObject media in (JArray)jt)
                                {
                                    if (media.TryGetValue("content_type", out JToken ct))
                                    {
                                        var contentType = ct.ToString();

                                        if (contentType.StartsWith("image"))
                                        {
                                            try
                                            {
                                                if (mode == IndexMode.IndexAll || mode == IndexMode.IndexImages || mode == IndexMode.IndexVideosAndImages)
                                                    if (media.TryGetValue("url", out JToken jurl))
                                                    {
                                                        var url = jurl.ToString();
                                                        if (alreadyIndexed.Contains(url) == false)
                                                        {
                                                            alreadyIndexed.Add(url);
                                                            result.Add(new Attachment(AttachmentType.Image, url, timeStamp, author));
                                                        }
                                                    }
                                            }
                                            catch { }
                                        }
                                        else
                                        if (contentType.StartsWith("video"))
                                        {
                                            try
                                            {
                                                if (mode == IndexMode.IndexAll || mode == IndexMode.IndexVideos || mode == IndexMode.IndexVideosAndImages)
                                                    if (media.TryGetValue("url", out JToken jurl))
                                                    {
                                                        var url = jurl.ToString();
                                                        if (alreadyIndexed.Contains(url) == false)
                                                        {
                                                            alreadyIndexed.Add(url);
                                                            result.Add(new Attachment(AttachmentType.Video, url, timeStamp, author));
                                                        }
                                                    }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }

                            if ((mode == IndexMode.IndexAll || mode == IndexMode.IndexText) && j.ContainsKey("content"))
                            {
                                try
                                {
                                    var content = j.GetValue("content").ToString();
                                    if (string.IsNullOrWhiteSpace(content) == false)
                                    {
                                        result.Add(new Attachment(AttachmentType.Text, content, timeStamp, author));
                                    }
                                }
                                catch { }
                            }

                            if (j.TryGetValue("id", out JToken mjt))
                            {
                                lastMessageID = mjt.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                };

                parseMessages();

                counter += messages.Count;

                // only enter this scope if there is *actually* more messages
                if (messages.Count >= 50)
                {
                    OnProgressUpdated?.Invoke(100.0 / max * counter);

                    while (true)
                    {
                        try
                        {
                            var limit = Math.Min(max - counter, 50);

                            if (limit == 0) // sending a limit of 0 will result in an error from the api
                                break;

                            data = await client.DownloadStringTaskAsync($"https://discord.com/api/v9/channels/{ChannelID}/messages?before={lastMessageID}&limit={limit}");
                            messages = (JArray)JsonConvert.DeserializeObject(data);

                            counter += messages.Count;

                            OnProgressUpdated?.Invoke(100.0 / max * counter);

                            parseMessages();

                            if (messages.Count != 50) // if it doesnt return 50, we have reached the end of the chat.
                                break;
                        }
                        catch { }
                    }
                }
                else
                    OnProgressUpdated?.Invoke(100.0 / messages.Count * counter);

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
