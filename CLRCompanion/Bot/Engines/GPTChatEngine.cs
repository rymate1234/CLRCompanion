using Azure.AI.OpenAI;
using CLRCompanion.Bot.Services;
using CLRCompanion.Data;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace CLRCompanion.Bot.Engines
{
    public class GPTChatEngine : BaseEngine
    {
        private string prompt = @"You are a discord bot designed to perform different prompts. The following will contain:
 - the prompt -- you should follow this as much as possible
 - at least one message from the channel, in the format [timestamp] <username>: message
 - If a message has embeds or attachments, they will be included in the prompt as well under the message as [embed] or [attachment]
Please write a suitable reply, only replying with the message

The prompt is as follows:";

        private readonly OpenAIService _api;

        public GPTChatEngine(IServiceProvider services, OpenAIService api) : base(services)
        {
            _api = services.GetRequiredService<OpenAIService>();
        }

        private Regex rgx = new Regex("[^a-zA-Z0-9_]");

        public void HandleCombinedMessages(IEnumerable<IMessage>? messages, List<ChatMessage> chatMessages, Data.Bot bot)
        {
            foreach (var message in messages)
            {
                var isBot = message.Author.Username == bot.Username && (message.Author.IsBot || message.Author.IsWebhook);
                var role = isBot ? ChatRole.Assistant : ChatRole.User;
                var lastMessage = chatMessages.LastOrDefault();

                // format date as yyyy-MM-dd HH:mm:ss
                var timestamp = message.Timestamp.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                var content = bot.CanPingUsers ? message.Content : message.CleanContent;
                var messageText = isBot ? content : $"[{timestamp}] {message.Author.Username}: {content}";

                foreach (var attachment in message.Attachments)
                {
                    messageText += $"\n[attachment] {attachment.Url} {attachment.Description}";
                }

                foreach (var embed in message.Embeds)
                {
                    messageText += $"\n[embed] {embed.Url} {embed.Title} {embed.Description}";
                }

                if (lastMessage != null && lastMessage.Role == ChatRole.User && role == ChatRole.User)
                {
                    lastMessage.Content += $"\n{messageText}";
                }
                else
                {
                    chatMessages.Add(new ChatMessage()
                    {
                        Role = role,
                        Content = messageText,
                    });
                }
            }
        }

        public void HandleMessagePerUser(IEnumerable<IMessage>? messages, List<ChatMessage> chatMessages, Data.Bot bot)
        {
            foreach (var message in messages)
            {
                var isBot = message.Author.Username == bot.Username && (message.Author.IsBot || message.Author.IsWebhook);
                var role = isBot ? ChatRole.Assistant : ChatRole.User;
                var lastMessage = chatMessages.LastOrDefault();
                var messageText = bot.CanPingUsers ? message.Content : message.CleanContent;
                // Username may contain a-z, A-Z, 0-9, and underscores, with a maximum length of 64 characters.
                var username = message.Author.Username;
                username = rgx.Replace(username, "");

                foreach (var attachment in message.Attachments)
                {
                    messageText += $"\n[attachment] {attachment.Url} {attachment.Description}";
                }

                foreach (var embed in message.Embeds)
                {
                    messageText += $"\n[embed] {embed.Url} {embed.Title} {embed.Description}";
                }

                if (lastMessage != null && lastMessage.Name == username)
                {
                    lastMessage.Content += $"\n{messageText}";
                }
                else
                {
                    chatMessages.Add(new ChatMessage()
                    {
                        Role = role,
                        Content = messageText,
                        Name = isBot ? bot.Username : username,
                    });
                }
            }
        }


        public override async Task<string> GetResponse(SocketMessage arg, Data.Bot bot)
        {
            var messages = await GetMessages(arg, bot);

            var chatMessages = new List<ChatMessage>();

            var prompt = bot.Prompt ?? "";

            if (!bot.FineTuned)
            {
                prompt = this.prompt + " " + bot.Prompt;
            }

            if (bot.CanPingUsers)
            {
                prompt += "The following people are in this conversation:";

                foreach (var message in messages)
                {
                    if (message.Author.IsBot || message.Author.IsWebhook)
                    {
                        continue;
                    }

                    var username = message.Author.Username;
                    username = rgx.Replace(username, "");

                    if (!prompt.Contains(message.Author.Id.ToString()))
                    {
                        prompt += $"\n - <@{message.Author.Id}> {message.Author.Username}";
                        
                        if (!bot.FineTuned)
                        {
                            prompt += $" ({username})";
                        }
                    }

                    if (message is RestUserMessage && ((RestUserMessage)message).MentionedUsers.Count > 0)
                    {
                        foreach (var user in ((RestUserMessage)message).MentionedUsers)
                        {
                            username = rgx.Replace(user.Username, "");
                            if (!prompt.Contains(user.Id.ToString()))
                            {
                                prompt += $"\n - <@{user.Id}> {user.Username}";

                                if (!bot.FineTuned)
                                {
                                    prompt += $" ({username})";
                                }
                            }
                        }
                    }

                    if (message is SocketUserMessage && ((SocketUserMessage)message).MentionedUsers.Count > 0)
                    {
                        foreach (var user in ((SocketUserMessage)message).MentionedUsers)
                        {
                            username = rgx.Replace(user.Username, "");
                            if (!prompt.Contains(user.Id.ToString()))
                            {
                                prompt += $"\n - <@{user.Id}> {user.Username}";

                                if (!bot.FineTuned)
                                {
                                    prompt += $" ({username})";
                                }
                            }
                        }
                    }
                }

                if (!bot.FineTuned)
                {
                    prompt += "\nUse the <@id> to ping them in the chat. Include the angle brackets, and the ID must be numerical.";
                }
            }

            chatMessages.Add(new ChatMessage(ChatRole.System, prompt));

            if (bot.MessagePerUser == true)
            {
                HandleMessagePerUser(messages, chatMessages, bot);
            }
            else
            {
                HandleCombinedMessages(messages, chatMessages, bot);
            }

            foreach (var message in chatMessages)
            {
                Console.WriteLine($"{message.Role} <{message.Name}> {message.Content}");
            }

            if (bot.Primer != null)
            {
                // todo: add primer to the history by doing an openai function call
            }

            if (bot.ResponseTemplate != null)
            {
                // todo: use openai function call to create a templated message
            }
            else
            {

            }

            var options = new ChatCompletionsOptions(chatMessages);
            var response = await _api.GetChatCompletionsAsync(bot.Model, options);

            // filter out any initial timestamp from the response
            var msg = response.Value.Choices[0].Message.Content;
            var filteredMsg = msg.Contains("> ") ? msg.Substring(msg.IndexOf("> ") + 2) : msg;

            return filteredMsg ?? "";
        }
    }
}
