namespace CLRCompanion.Data
{
    public class Bot
    {
        public int Id { get; set; }
        public ulong ChannelId { get; set; }
        public ulong? WebhookId { get; set; }
        public string Username { get; set; }
        public string Prompt { get; set; }
        public string Model { get; set; }
        public double Chance { get; set; }
        public int Limit { get; set; }
        public string? AvatarUrl { get; set; }
        public bool Default { get; set; }

        public string TruncatedPrompt
        {
            get => Prompt.Length > 50 ? Prompt.Substring(0, 50) + "..." : Prompt;
        }
    }
}
    