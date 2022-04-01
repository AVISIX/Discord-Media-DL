using Discord_Media_DL.Core;
using Discord_Media_DL.Discord;
using Discord_Media_DL.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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

            switch(Console.ReadLine().ToLower()[0])
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

        public static Task<Task> Run()
        {
            /*
            To Do:
            - Make different folders for image and video
            */

            PrintWaterMark();

            string token = null;
            string channel = null;
            string outputPath = KnownFolders.GetPath(KnownFolder.Downloads);

#if DEBUG
            token = "";
            channel = "754718093984530543";
#endif

            #region Token Stuff
            while (token == null)
            {
                Console.WriteLine();

                try
                {
                    YesNo("Do you already have a Token?",
                        () => {
                            Console.WriteLine("Please enter your Token:");

                            string temp = Console.ReadLine().Replace(" ", "");

                            Console.WriteLine("Checking Token...");

                            if (Token.IsValidToken(temp).Result == false)
                            {
                                Console.WriteLine("Invalid Token. Please try again.");
                                return;
                            }

                            token = temp;
                        },

                        () => {
                            Console.WriteLine();

                            YesNo("Do you want me to search for a Token?",
                                () => {
                                    Console.WriteLine("Searching for Tokens...");

                                    string[] tokens = Token.FindAllTokens().GetAwaiter().GetResult();

                                    if(tokens.Length == 0)
                                    {
                                        Console.WriteLine("No Tokens found.");
                                        return;
                                    }

                                askAgain:

                                    Console.WriteLine("\nWhich of these Tokens is the right one? [1,2,3,...]");

                                    Console.ForegroundColor = ConsoleColor.Yellow;

                                    {
                                        List<string> alreadyListed = new();

                                        for (int i = 0; i < tokens.Length; i++)
                                        {
                                            Token.DiscordUser user = Token.GetUserByToken(tokens[i]).Result.Value;

                                            string username = user.Name + "#" + user.Discriminator;

                                            if (alreadyListed.Contains(username))
                                                continue;

                                            alreadyListed.Add(username);

                                            Console.WriteLine($"> {i + 1}. Username: '{username}' | Token: '{user.Token}'");

                                            //      Console.WriteLine(user.Token);
                                        }
                                    }

                                    Console.ForegroundColor = ConsoleColor.White;

                                    {
                                        string answer = Console.ReadLine();
                                    
                                        if (string.IsNullOrEmpty(answer))
                                            goto askAgain;

                                        try
                                        {
                                            int number = int.Parse(answer[0].ToString());

                                            number--;

                                            if (number >= 0 && number < tokens.Length)
                                            {
                                                token = tokens[number];

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
                                    }
                                },
                                () => {
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

                    string temp = Console.ReadLine();

                    if(Channel.IsValid(temp, token).Result == false)
                    {
                        Console.WriteLine("This Channel doesn't exist or you don't have access to it.\n");
                        continue;
                    }

                    channel = temp;
                }
                catch(Exception e)
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

                string temp = Console.ReadLine();

                if(string.IsNullOrEmpty(temp))
                {
                    outputPath += "\\DiscDL_" + channel + "\\";

                    if (Directory.Exists(outputPath) == false)
                        Directory.CreateDirectory(outputPath);
                }
                else
                {
                    if(Directory.Exists(temp) == false)
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
                int maxDownloads = 1000;

#region Get Max Indexing Depth
                {
                    Console.WriteLine("How many messages do you want to index? [Press Enter for Default: 1000]:");

                    string temp = Console.ReadLine();

                    if (string.IsNullOrEmpty(temp) == false)
                    {
                        if (int.TryParse(temp, out int mxd))
                            maxDownloads = mxd;
                        else
                            Console.WriteLine("Invalid Download Amount. Using default: 1000");
                    }
                }
#endregion
#else
                int maxDownloads = 1000;
#endif

                Console.WriteLine();

                ChannelReader.IndexMode mode = ChannelReader.IndexMode.IndexAll;

#region Get Index Mode
#if RELEASE
                {
                getModeAgain:

                    Console.WriteLine("What exactly do you want to Download? [1,2,3,...]");

                    var names = Enum.GetNames(typeof(ChannelReader.IndexMode));
                
                    for(int i = 0; i < names.Length; i++)
                    {
                        Console.WriteLine($"{i+1}. {names[i]}");
                    }

                    string temp = Console.ReadLine();

                    if (int.TryParse(temp, out int t))
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
                    if (e is WebException == false)
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
                    MediaDownloader dl = new MediaDownloader();

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

                    List<string> ImageUrls = new List<string>();
                    List<string> VideoUrls = new List<string>();

                    foreach (Attachment m in media)
                    {
                        switch (m.Type)
                        {
                            case AttachmentType.Image:
                                ImageUrls.Add(m.Url);
                                break;

                            case AttachmentType.Video:
                                VideoUrls.Add(m.Url);
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
                        Console.WriteLine("Downloading Vidoes...");

                        dl.Download(VideoUrls.ToArray(), outputPath + @"\videos\").Result.GetAwaiter().GetResult();
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

            return Task.FromResult(Task.CompletedTask);
        }

        static void Main(string[] args) => Run().Wait();
    }
}
