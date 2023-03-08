namespace Discord_Media_DL.Token
{
    // Discord Token struct. We can also use Class here, but its good to use structs for this kind of stuff.
    struct DiscordToken
    {
        public DiscordToken(IApp Origin, string Token)
        {
            this.Origin = Origin;
            this.Token = Token;
        }

        // The Origin of the Token, so we can later identify where it came from.
        public IApp Origin { get; }

        // The token itself. 
        public string Token { get; }
    }
}
