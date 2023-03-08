using System.Threading.Tasks;

namespace Discord_Media_DL.Token
{
    /// <summary>
    /// A Discord Application
    /// </summary>
    interface IApp
    {
        /// <summary>
        /// The Name of the Application.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// All the Paths that may contain a Discord Token.
        /// </summary>
        string[] TokenPaths { get; }

        /// <summary>
        /// Get all the Tokens from the specific Paths asynchronously. 
        /// </summary>
        /// <returns></returns>
        Task<string[]> GetTokens();
    }
}
