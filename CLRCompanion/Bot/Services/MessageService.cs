using CLRCompanion.Data;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenAI_API;
using OpenAI_API.Chat;
using Slugify;
using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using static System.Formats.Asn1.AsnWriter;

namespace CLRCompanion.Bot.Services
{
    public class MessageService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
        private readonly OpenAIAPI _api;
        private SlugHelper helper = new SlugHelper();

        private string prompt = @"
You are a discord bot designed to perform different prompts. The following will contain:
 - the prompt -- you should follow this as much as possible
 - at least one message from the channel, in the format [timestamp] <username>: message
 - If a message has embeds or attachments, they will be included in the prompt as well under the message as [embed] or [attachment]
Please write a suitable reply, only replying with the message

The prompt is as follows:";

        public MessageService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _api = services.GetRequiredService<OpenAIAPI>();
            _services = services;
        }

        public void Initialize()
        {
            _discord.MessageReceived += _discord_MessageReceived;
        }

        private Task _discord_MessageReceived(SocketMessage arg)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await MessageReceived(arg);
                } catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            List<Data.Bot> bots = new List<Data.Bot>();
            using (var scope = _services.CreateScope())
            {

                var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // ignore messages from the bot
                if (arg.Author.Id == _discord.CurrentUser.Id && arg.Interaction?.Name != "ask")
                {
                    return;
                }

                // find all bots in the channel
                bots = await _dbContext.Bots.Where(b => b.ChannelId == arg.Channel.Id).ToListAsync();

                // if there are no bots in the channel, return
                if (bots.Count() == 0)
                {
                    return;
                }

                if (arg.MentionedUsers.Any(u => u.Id == _discord.CurrentUser.Id))
                {
                    var defaultBot = await _dbContext.Bots.FirstOrDefaultAsync(b => b.ChannelId == arg.Channel.Id && b.Default);

                    if (defaultBot != null)
                    {
                        await HandleReply(arg, defaultBot);
                        return;
                    }

                    // reply to the user with a message
                    await arg.Channel.SendMessageAsync("Hey, you'll want to ping one of the bots not me directly, use `/list` or `/ask`");
                    return;
                }
            }

            // check the message for a bot ping starting with @ e.g. @bot
            var bot = bots.FirstOrDefault
            (
                b => arg.CleanContent.Contains($"@{b.Username}")
                  || arg.MentionedUsers.Any(m => m.Username == b.Username)
                  || arg is SocketUserMessage message && ((SocketUserMessage)arg).ReferencedMessage?.Author.Username == b.Username
            );

            // if there is no bot ping, return
            if (bot == null)
            {
                bot = HandleChance(arg, bots);
                if (bot == null) return;
            }

            await HandleReply(arg, bot);
        }

        private Data.Bot? HandleChance(SocketMessage arg, List<Data.Bot> bots)
        {
            // randomise the array first
            bots = bots.OrderBy(b => Guid.NewGuid()).ToList();

            // get bot via random chance
            var bot = bots.FirstOrDefault(b => b.Chance >= new Random().NextDouble() && arg.Author.Username != b.Username);

            return bot;
        }

        private async Task<DiscordWebhookClient> GetWebhookAsync(IIntegrationChannel channel, Data.Bot bot)
        {
            IWebhook webhook = null;
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dbChannel = dbContext.Channels.FirstOrDefault(c => c.Id == channel.Id);

                if (dbChannel == null)
                {
                    dbChannel = new Data.Channel
                    {
                        Id = channel.Id,
                        WebhookId = null
                    };
                    dbContext.Channels.Add(dbChannel);
                }

                if (dbChannel.WebhookId != null)
                {
                    webhook = await channel.GetWebhookAsync(dbChannel.WebhookId.Value);
                }

                if (webhook == null)
                {
                    webhook = await channel.CreateWebhookAsync(bot.Username);
                }

                dbChannel.WebhookId = webhook.Id;
                await dbContext.SaveChangesAsync();
            }

            return new DiscordWebhookClient(webhook);
        }

        private async Task HandleReply(SocketMessage arg, Data.Bot bot)
        {
            await arg.Channel.TriggerTypingAsync();
            using var disposable = arg.Channel.EnterTypingState();

            var messages = await arg.Channel.GetMessagesAsync(bot.Limit).FlattenAsync();

            // rid the messages of the bot's messages including "I'm sorry" or "as an AI language model"
            var filtered = messages
                .Where(m => !m.Author.IsBot || (m.Author.IsBot && !m.Content.Contains("I'm sorry") && !m.Content.Contains("as an AI language model")))
                .Reverse();

            var previousMessage = filtered.LastOrDefault();

            if (arg.Id != previousMessage?.Id)
            {
                Console.WriteLine("Message not found");
                filtered = filtered.Append(arg);
            }

            var chatMessages = new List<ChatMessage>();

            if (bot.Prompt != "")
            {
                chatMessages.Add(new ChatMessage(ChatMessageRole.System, prompt + "\n\n" + bot.Prompt));
            }

            foreach (var message in filtered)
            {
                var isBot = message.Author.Username == bot.Username && (message.Author.IsBot || message.Author.IsWebhook);
                var role = isBot ? ChatMessageRole.Assistant : ChatMessageRole.User;
                var userSlug = helper.GenerateSlug(message.Author.Username);
                var lastMessage = chatMessages.LastOrDefault();

                var messageText = isBot ? message.CleanContent : $"[{DateTime.UtcNow}] <{message.Author.Username}> {message.CleanContent}";

                foreach (var attachment in message.Attachments)
                {
                    messageText += $"\n[attachment] {attachment.Url} {attachment.Description}";
                }

                foreach(var embed in message.Embeds)
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
                        Content = messageText,
                        Name = userSlug
                    });
                }
            }

            var response = await _api.Chat.CreateChatCompletionAsync(chatMessages, bot.Model);

            // filter out any initial timestamp from the response
            var msg = response.ToString();
            var filteredMsg = msg.Contains("> ") ? msg.Substring(msg.IndexOf("> ") + 2) : msg;

            var channel = arg.Channel as IIntegrationChannel;

            if (channel != null)
            {
                var client = await GetWebhookAsync(channel, bot);
                var url = _discord.CurrentUser.GetAvatarUrl();

                await client.SendMessageAsync(filteredMsg, avatarUrl: bot.AvatarUrl ?? url, username: bot.Username);
            }
            else
            {
                await arg.Channel.SendMessageAsync(filteredMsg);
            }
        }
    }
}
