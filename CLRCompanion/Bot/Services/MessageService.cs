using CLRCompanion.Bot.Engines;
using CLRCompanion.Data;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;

namespace CLRCompanion.Bot.Services
{
    public class MessageService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public MessageService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
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
                }
                catch (Exception ex)
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
                b => b.DidMention(arg) && !b.IgnorePings
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

        public BaseEngine GetEngine(ModelType type)
        {
            using var scope = _services.CreateScope();
            switch (type)
            {
                case ModelType.GPTText:
                    return scope.ServiceProvider.GetRequiredService<GPTTextEngine>();
                case ModelType.GPTChat:
                    return scope.ServiceProvider.GetRequiredService<GPTChatEngine>();
                case ModelType.Endpoint:
                    return scope.ServiceProvider.GetRequiredService<EndpointEngine>();
                default:
                    throw new ArgumentException("Invalid type", "type");
            }
        }

        private async Task HandleReply(SocketMessage arg, Data.Bot bot)
        {
            await arg.Channel.TriggerTypingAsync();
            using var disposable = arg.Channel.EnterTypingState();

            await Task.Delay(500);

            var engine = GetEngine(bot.ModelType);

            var filteredMsg = await engine.GetResponse(arg, bot);

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
