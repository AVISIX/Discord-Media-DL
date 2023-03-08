using Newtonsoft.Json;

namespace Discord_Media_DL.Token
{
    // Some Discord User
    struct DiscordUser
    {
        // The Token associated with this DiscordUser
        public DiscordToken Token { get; set; }

        // The Payment Info associated with this Account. 
        public DiscordPaymentInfo[] PaymentInfo { get; set; }

        // The ID of the Discord User
        public string ID { get; set; }

        // The Name of the Discord User
        public string Name { get; set; }

        // The Discriminator of the Discord User (The Numbers after the #)
        public string Discriminator { get; set; }

        // The Email of the Discord User.
        public string Mail { get; set; }

        // The Phone Number, if connected.
        public string Phone { get; set; }

        // The Locale associated with this Discord Account. 
        public string Locale { get; set; }

        // The Url to the Avatar/Profile-Picture of this Account. 
        public string Avatar { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
