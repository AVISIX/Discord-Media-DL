using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Discord_Media_DL.Core;
using Discord_Media_DL.Discord;
using Discord_Media_DL.Misc;
using Discord_Media_DL.Token;

namespace Discord_Media_DL
{
    class Program
    {
        public const int DownloadThreads = 8;

        static void PrintWaterMark()
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(
@"
________  .__                              .___
\______ \ |__| ______ ____  ___________  __| _/
 |    |  \|  |/  ___// ___\/  _ \_  __ \/ __ | 
 |    `   \  |\___ \\  \__(  <_> )  | \/ /_/ | 
/_______  /__/____  >\___  >____/|__|  \____ | 
        \/        \/     \/                 \/ 
   _____             .___.__        
  /     \   ____   __| _/|__|____   
 /  \ /  \_/ __ \ / __ | |  \__  \  
/    Y    \  ___// /_/ | |  |/ __ \_
\____|__  /\___  >____ | |__(____  /
        \/     \/     \/         \/ 
________                      .__                    .___
\______ \   ______  _  ______ |  |   _________     __| _/
 |    |  \ /  _ \ \/ \/ /    \|  |  /  _ \__  \   / __ | 
 |    `   (  <_> )     /   |  \  |_(  <_> ) __ \_/ /_/ | 
/_______  /\____/ \/\_/|___|  /____/\____(____  /\____ | 
        \/                  \/                \/      \/ 
");

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Author: github.com/sixmax");

            Console.ForegroundColor = ConsoleColor.White;
        }

        static void YesNo(string question, Action Yes, Action No)
        {
        retry:

            Console.WriteLine(question + "[Y/N]");

            switch (Console.ReadLine().ToLower()[0])
            {
                case 'y': Yes(); break;
                case 'n': No(); break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid Answer, only Y/N/Yes/No is accepted!\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    goto retry;
            }
        }

        public static async Task<Task> Run()
        {
            /*
            To Do:
            - Make different folders for image and video
            */

            PrintWaterMark();

            string token = null;
            string channel = null;
            var outputPath = KnownFolders.GetPath(KnownFolder.Downloads);

            #region Token Stuff
            while (token == null)
            {
                Console.WriteLine();

                try
                {
                    YesNo("Do you already have a Token?",
                        () =>
                        {
                            Console.WriteLine("Please enter your Token:");

                            var temp = Console.ReadLine().Replace(" ", "");

                            Console.WriteLine("Checking Token...");

                            if (TokensUtil.IsValidToken(temp).Result == false)
                            {
                                Console.WriteLine("Invalid Token. Please try again.");
                                return;
                            }

                            token = temp;
                        },

                        () =>
                        {
                            Console.WriteLine();

                            YesNo("Do you want me to search for a Token?",
                                () =>
                                {
                                    Console.WriteLine("Searching for Tokens...");
                                    Console.WriteLine("Please wait, this might take a moment.");

                                    List<DiscordToken> tokens = TokenSearcher.GetTokensAsync().Result;

                                    if (tokens.Count == 0)
                                    {
                                        Console.WriteLine("No Tokens found.");
                                        return;
                                    }

                                    List<DiscordUser> users = TokenSearcher.FilterTokensAsync(tokens).Result;

                                askAgain:
                                    Console.WriteLine("\nWhich of these Tokens is the right one? [1,2,3,...]");
                                    Console.ForegroundColor = ConsoleColor.Yellow;

                                    var i = 1;
                                    foreach (DiscordUser user in users)
                                    {
                                        Console.WriteLine($"> {i}. Username: '{user.Name}' | Token: '{user.Token.Token}'");
                                        i++;
                                    }

                                    Console.ForegroundColor = ConsoleColor.White;

                                    var answer = Console.ReadLine();

                                    if (string.IsNullOrEmpty(answer))
                                        goto askAgain;

                                    try
                                    {
                                        var number = int.Parse(answer[0].ToString());

                                        number--;

                                        if (number >= 0 && number < users.Count)
                                        {
                                            token = users[number].Token.Token;
#if DEBUG
                                            Console.WriteLine("Token: " + token);
#endif
                                        }
                                        else
                                        {
                                            Console.WriteLine("Invalid Answer.");
                                            return;
                                        }
                                    }
                                    catch (Exception e)
                                    {
#if DEBUG
                                        Console.WriteLine(e);
#endif
                                    }
                                },
                                () =>
                                {
                                    Console.WriteLine();
                                });
                        });
                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine(e);
#endif
                }
            }
            #endregion

            Console.WriteLine();

        repeat:

            #region Channel stuff
            while (channel == null)
            {
                try
                {
                    Console.WriteLine("Enter the Channel ID you want to download Media from:");

                    var temp = Console.ReadLine();

                    if (Channel.IsValid(temp, token).Result == false)
                    {
                        Console.WriteLine("This Channel doesn't exist or you don't have access to it.\n");
                        continue;
                    }

                    channel = temp;
                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine(e);
#endif
                }
            }
            #endregion

            Console.WriteLine();

            #region Output Dir
#if RELEASE
            {
            getDirAgain:

                Console.WriteLine("Enter the Directory into which the Media should get downloaded.");
                Console.WriteLine("Press Enter to use Download Directory:");

                var temp = Console.ReadLine();

                if (string.IsNullOrEmpty(temp))
                {
                    outputPath += "\\DiscDL_" + channel + "\\";

                    if (Directory.Exists(outputPath) == false)
                        Directory.CreateDirectory(outputPath);
                }
                else
                {
                    if (Directory.Exists(temp) == false)
                    {
                        Console.WriteLine("Invalid Directory, try again.\n");
                        goto getDirAgain;
                    }
                }
            }
#else
            outputPath += "\\DiscDL_" + channel + "\\";
            if (Directory.Exists(outputPath) == false)
                Directory.CreateDirectory(outputPath);
#endif
            #endregion

            Console.WriteLine();

            Attachment[] media = new Attachment[] { };

            #region Index Media Url's
            {

#if RELEASE
                var maxDownloads = 1000;

                #region Get Max Indexing Depth
                {
                    Console.WriteLine("How many messages do you want to index? [Press Enter for Default: 1000]:");

                    var temp = Console.ReadLine();

                    if (string.IsNullOrEmpty(temp) == false)
                    {
                        if (int.TryParse(temp, out var mxd))
                            maxDownloads = mxd;
                        else
                            Console.WriteLine("Invalid Download Amount. Using default: 1000");
                    }
                }
                #endregion
#else
                var maxDownloads = 1000;
#endif

                Console.WriteLine();

                ChannelReader.IndexMode mode = ChannelReader.IndexMode.IndexAll;

                #region Get Index Mode
#if RELEASE
                {
                getModeAgain:

                    Console.WriteLine("What exactly do you want to Download? [1,2,3,...]");

                    var names = Enum.GetNames(typeof(ChannelReader.IndexMode));

                    for (var i = 0; i < names.Length; i++)
                    {
                        Console.WriteLine($"{i + 1}. {names[i]}");
                    }

                    var temp = Console.ReadLine();

                    if (int.TryParse(temp, out var t))
                    {
                        t--;

                        try
                        {
                            mode = (ChannelReader.IndexMode)Enum.Parse(typeof(ChannelReader.IndexMode), names[t]);
                        }
                        catch
                        {
                            Console.WriteLine("Invalid value, try again.");
                            goto getModeAgain;
                        }
                    }
                    else
                        goto getModeAgain;
                }
#endif
                #endregion
                Console.WriteLine();

                Console.WriteLine("Indexing Media...");

                ChannelReader reader = new(channel, token);

                reader.OnProgressUpdated += new ChannelReaderProgressUpdateHandler((double progress) =>
                {
                    Console.WriteLine($"Progress: {decimal.Round((decimal)progress, 1)}%");
                });

                reader.OnError += new ChannelReaderErrorHandler((Exception e) =>
                {
                    if ((e is WebException) == false)
                        return;

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                });

                media = reader.IndexAttachments(maxDownloads, mode).GetAwaiter().GetResult();

                Console.WriteLine($"Finished Indexing, total Media indexed: {media.Length}");
            }
            #endregion

            Console.WriteLine();

            if (media.Length > 0)
            {
                #region Download Media
                {
                    MediaDownloader dl = new();

                    dl.OnDownloadError += new MediaDownloaderErrorHandler((Exception e, string url) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to Download '{url.GetFilenameFromUrl()}'. Reason: " + e.Message);
                        Console.ForegroundColor = ConsoleColor.White;
                    });

                    dl.OnDownloaded += new MediaDownloaderMediaDownloaded((string url, double progress) =>
                    {
                        Console.WriteLine($"Progress: {decimal.Round((decimal)progress, 1)}% | Downloaded '{url.GetFilenameFromUrl()}'");
                    });

                    List<string> ImageUrls = new();
                    List<string> VideoUrls = new();
                    List<Attachment> TextMessages = new();

                    foreach (Attachment m in media)
                    {
                        switch (m.Type)
                        {
                            case AttachmentType.Image:
                                ImageUrls.Add(m.Content);
                                break;

                            case AttachmentType.Video:
                                VideoUrls.Add(m.Content);
                                break;

                            case AttachmentType.Text:
                                TextMessages.Add(m);
                                break;
                        }
                    }

                    if (ImageUrls.Count > 0)
                    {
                        Console.WriteLine("Downloading Images...");
                        dl.Download(ImageUrls.ToArray(), outputPath + @"\images\").Result.GetAwaiter().GetResult();
                    }

                    if (VideoUrls.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Downloading Videos...");
                        dl.Download(VideoUrls.ToArray(), outputPath + @"\videos\").Result.GetAwaiter().GetResult();
                    }

                    if (TextMessages.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Downloading Message History...");

                        var historyFile = Path.Combine(outputPath, "history.txt");

                        if (File.Exists(historyFile) == true)
                            File.Delete(historyFile);

                        TextMessages.Reverse();

                        using StreamWriter fs = File.CreateText(historyFile);
                        foreach (Attachment message in TextMessages)
                        {
                            fs.WriteLine($"[ --- {message.Author.Name}#{message.Author.Discriminator} on {message.TimeStamp} --- ]");
                            fs.WriteLine(message.Content + "\n\n");
                        }
                    }
                }
                #endregion
            }
            else
            {
                Console.WriteLine("No Media found.");
            }

            Console.WriteLine();

            Console.WriteLine("Done.");

            Console.WriteLine();

            channel = null;

            goto repeat;
        }

        static void Main(string[] args)
        {
            Run().Wait();
        }
    }
}
