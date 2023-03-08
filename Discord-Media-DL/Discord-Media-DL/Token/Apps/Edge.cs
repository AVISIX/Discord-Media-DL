using System.Threading.Tasks;

#pragma warning disable CS1998

namespace Discord_Media_DL.Token.Apps
{
#warning Add Microsoft-Edge Browser Support 

    class Edge : IApp
    {
        public string Name => "Microsoft Edge";

        public string[] TokenPaths => new string[]
        {
        };

        public async Task<string[]> GetTokens()
        {
            return new string[] { };
        }
    }
}
