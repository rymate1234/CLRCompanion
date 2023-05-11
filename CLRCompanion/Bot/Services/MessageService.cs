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
using System.Reflection;
using System.Text;

namespace CLRCompanion.Bot.Services
{
    public class MessageService
    {
        private readonly DiscordSocketClient _discord;
        private readonly ApplicationDbContext _dbContext;
        private readonly OpenAIAPI _api;
        private SlugHelper helper = new SlugHelper();

        public MessageService(IServiceProvider services)
        {
            _dbContext = services.GetRequiredService<ApplicationDbContext>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _api = services.GetRequiredService<OpenAIAPI>();
        }

        public void Initialize()
        {
            _discord.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            // ignore messages from the bot
            if (arg.Author.Id == _discord.CurrentUser.Id && arg.Interaction?.Name != "ask")
            {
                return;
            }

            // find all bots in the channel
            var bots = await _dbContext.Bots.Where(b => b.ChannelId == arg.Channel.Id).ToListAsync();

            // if there are no bots in the channel, return
            if (bots.Count() == 0)
            {
                return;
            }

            var defaultBot = await _dbContext.Bots.FirstOrDefaultAsync(b => b.ChannelId == arg.Channel.Id && b.Default);

            if (arg.MentionedUsers.Any(u => u.Id == _discord.CurrentUser.Id))
            {
                if (defaultBot != null)
                {
                    await HandleReply(arg, defaultBot);
                    return;
                }

                // reply to the user with a message
                await arg.Channel.SendMessageAsync("Hey, you'll want to ping one of the bots not me directly, use `/list` or `/ask`");
                return;
            }


            // check the message for a bot ping starting with @ e.g. @bot
            var botPing = bots.FirstOrDefault
            (
                b => arg.CleanContent.Contains($"@{b.Username}")
                  || arg.MentionedUsers.Any(m => m.Username == b.Username)
                  || arg is SocketUserMessage message && ((SocketUserMessage)arg).ReferencedMessage?.Author.Username == b.Username
            );

            // if there is no bot ping, return
            if (botPing == null)
            {
                await HandleChance(arg, bots);
                return;
            }

            await HandleReply(arg, botPing);
        }

        private async Task HandleChance(SocketMessage arg, List<Data.Bot> bots)
        {
            // randomise the array first
            bots = bots.OrderBy(b => Guid.NewGuid()).ToList();

            // get bot via random chance
            var bot = bots.FirstOrDefault(b => b.Chance >= new Random().NextDouble() && arg.Author.Username != b.Username);

            // if there is no bot, return
            if (bot == null)
            {
                return;
            }

            await HandleReply(arg, bot);
        }

        private async Task HandleReply(SocketMessage arg, Data.Bot bot)
        {
            IDisposable disposable = null;
            try
            {
                await arg.Channel.TriggerTypingAsync();
                disposable = arg.Channel.EnterTypingState();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                disposable?.Dispose();
                return;
            }

            var messages = await arg.Channel.GetMessagesAsync(bot.Limit).FlattenAsync();

            // rid the messages of the bot's messages including "I'm sorry" or "as an AI language model"
            var filtered = messages.Where(m => !m.Author.IsBot && !m.Content.Contains("I'm sorry") && !m.Content.Contains("as an AI language model"));

            var chatMessages = new List<ChatMessage>();

            if (bot.Prompt != "")
            {
                chatMessages.Add(new ChatMessage(ChatMessageRole.User, bot.Prompt));
            }

            foreach (var message in filtered)
            {
                var isBot = message.Author.Username == bot.Username && (message.Author.IsBot || message.Author.IsWebhook);
                var role = isBot ? ChatMessageRole.Assistant : ChatMessageRole.User;
                var userSlug = helper.GenerateSlug(message.Author.Username);
                chatMessages.Add(new ChatMessage()
                {
                    Role = role,
                    Content = message.Content,
                    Name = userSlug
                });
            }

            var response = await _api.Chat.CreateChatCompletionAsync(chatMessages, bot.Model);

            var channel = arg.Channel as IIntegrationChannel;
            if (channel != null)
            {
                var webhook = await channel.CreateWebhookAsync(bot.Username);
                var client = new DiscordWebhookClient(webhook);
                var url = _discord.CurrentUser.GetAvatarUrl();

                await client.SendMessageAsync(response.ToString(), avatarUrl: bot.AvatarUrl ?? url);

                await webhook.DeleteAsync();
            }
            else
            {
                await arg.Channel.SendMessageAsync(response.ToString());
            }

            disposable.Dispose();
        }
    }
}
