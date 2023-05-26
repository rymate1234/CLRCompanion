using CLRCompanion.Data;
using CLRCompanion.Pages;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OpenAI_API;
using OpenAI_API.Chat;
using Slugify;

namespace CLRCompanion.Bot.Engines
{
    public class GPTChatEngine : BaseEngine
    {
        private string prompt = @"
You are a discord bot designed to perform different prompts. The following will contain:
 - the prompt -- you should follow this as much as possible
 - at least one message from the channel, in the format [timestamp] <username>: message
 - If a message has embeds or attachments, they will be included in the prompt as well under the message as [embed] or [attachment]
Please write a suitable reply, only replying with the message

The prompt is as follows:";

        private readonly OpenAIAPI _api;

        public GPTChatEngine(IServiceProvider services, OpenAIAPI api): base(services)
        {
            _api = services.GetRequiredService<OpenAIAPI>();
        }

        public override async Task<string> GetResponse(SocketMessage arg, Data.Bot bot)
        {
            var messages = await GetMessages(arg, bot);

            var chatMessages = new List<ChatMessage>();

            if (bot.Prompt != "")
            {
                chatMessages.Add(new ChatMessage(ChatMessageRole.System, prompt + "\n\n" + bot.Prompt));
            }

            foreach (var message in messages)
            {
                var isBot = message.Author.Username == bot.Username && (message.Author.IsBot || message.Author.IsWebhook);
                var role = isBot ? ChatMessageRole.Assistant : ChatMessageRole.User;
                var lastMessage = chatMessages.LastOrDefault();

                // format date as yyyy-MM-dd HH:mm:ss
                var timestamp = message.Timestamp.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                var messageText = isBot ? message.CleanContent : $"[{timestamp}] <{message.Author.Username}> {message.CleanContent}";

                foreach (var attachment in message.Attachments)
                {
                    messageText += $"\n[attachment] {attachment.Url} {attachment.Description}";
                }

                foreach (var embed in message.Embeds)
                {
                    messageText += $"\n[embed] {embed.Url} {embed.Title} {embed.Description}";
                }

                if (lastMessage != null && lastMessage.Role == ChatMessageRole.User && role == ChatMessageRole.User)
                {
                    lastMessage.Content += $"\n{messageText}";
                }
                else
                {
                    chatMessages.Add(new ChatMessage()
                    {
                        Role = role,
                        Content = messageText
                    });
                }
            }

            var response = await _api.Chat.CreateChatCompletionAsync(chatMessages, bot.Model);

            // filter out any initial timestamp from the response
            var msg = response.ToString();
            var filteredMsg = msg.Contains("> ") ? msg.Substring(msg.IndexOf("> ") + 2) : msg;

            return filteredMsg ?? "";
        }
    }
}
