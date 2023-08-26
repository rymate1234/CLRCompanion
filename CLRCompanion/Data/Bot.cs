using Discord;
using Discord.WebSocket;

namespace CLRCompanion.Data
{
    public class Bot
    {
        public int Id { get; set; }
        public ulong ChannelId { get; set; }
        public string Username { get; set; }
        public string Prompt { get; set; }
        public OpenAIFunction? Primer { get; set; }
        public OpenAIFunction? ResponseTemplate { get; set; }
        public string Model { get; set; }
        public ModelType ModelType { get; set; }
        public double Chance { get; set; }
        public int Limit { get; set; }
        public string? AvatarUrl { get; set; }
        public bool Default { get; set; }
        public bool IgnorePings { get; set; }
        public string? StopToken { get; set; }
        public string? PromptSuffix { get; set; }
        public bool MessagePerUser { get; set; }
        public bool FineTuned { get; set; }
        public bool CanPingUsers { get; set; }

        public string TruncatedPrompt
        {
            get => Prompt.Length > 50 ? Prompt.Substring(0, 50) + "..." : Prompt;
        }

        public bool DidMention(IMessage arg)
        {
            return (arg.CleanContent.Contains($"@{this.Username}")
                  || arg is SocketMessage && ((SocketMessage)arg).MentionedUsers.Any(m => m.Username == this.Username)
                  || arg is SocketUserMessage message && ((SocketUserMessage)arg).ReferencedMessage?.Author.Username == this.Username);
        }
    }

    public enum ModelType
    {
        GPTText, GPTChat, Endpoint
    }
}
    