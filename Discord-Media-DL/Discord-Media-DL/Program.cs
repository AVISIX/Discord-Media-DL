using Discord_Media_DL.Core;
using Discord_Media_DL.Discord;
using Discord_Media_DL.Misc;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
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
            PrintWaterMark();

            string token = null;
            string channel = null;
            string outputPath = KnownFolders.GetPath(KnownFolder.Downloads);

#if DEBUG
            token = "LOL YOU THOUGHT XD";
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

                            YesNo(@"Do you want me to search for a Token? 
If so, this will close all instances of Discord and your Browsers.",
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

                                    for(int i = 0; i < tokens.Length; i++)
                                    {
                                        string temp = tokens[i];
                                        Token.DiscordUser user = Token.GetUserByToken(temp).Result.Value;
                                        Console.WriteLine($"> {i + 1}. Username: {user.Name}#{user.Discriminator}");

                                  //      Console.WriteLine(user.Token);
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

            string[] media = new string[] { };

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
                    Console.WriteLine("Error: " + e.Message);
                });

                media = reader.IndexMedia(maxDownloads, mode).GetAwaiter().GetResult();

                Console.WriteLine($"Finished Indexing, total Media indexed: {media.Length}");
            }
#endregion

            Console.WriteLine();

            #region Download Media
            {
                Console.WriteLine("Downloading Media...");

                MediaDownloader dl = new MediaDownloader();

                dl.OnDownloadError += new MediaDownloaderErrorHandler((Exception e, string url) =>
                {
                    Console.WriteLine($"Failed to Download: {url}");
                });

                dl.OnProgressUpdated += new MediaDownloaderProgressUpdateHandler((double progress) =>
                {
                    Console.WriteLine($"Progress: {decimal.Round((decimal)progress, 1)}%");
                });

                dl.OnDownloaded += new MediaDownloaderMediaDownloaded((string url) =>
                {
                //    Console.WriteLine($"Downloaded: {url}");
                });

                dl.Download(media, outputPath).Result.GetAwaiter().GetResult();
            }
#endregion

            return Task.FromResult(Task.CompletedTask);
        }

        static void Main(string[] args) => Run().Wait();
    }
}
