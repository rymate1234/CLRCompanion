using CLRCompanion.Data;
using CLRCompanion.Pages;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CLRCompanion.Bot.Engines
{
    public abstract class BaseEngine
    {
        internal IServiceProvider _services;

        protected BaseEngine(IServiceProvider services)
        {
            this._services = services;
        }

        internal async Task<IEnumerable<IMessage>> GetMessages(SocketMessage arg, Data.Bot bot)
        {
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

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // remove messages from users who have opted out
                var optOut = await dbContext.UserPreferences
                    .Where(u => u.UserMessagePreference == UserMessagePreference.None)
                    .Select(u => u.Id)
                    .ToListAsync();

                filtered = filtered.Where(m =>
                {
                    return !optOut.Contains(m.Author.Id);
                });

                // remove messages from users who have it set to only mention and did not mention the bot
                var pingOnly = await dbContext.UserPreferences
                    .Where(u => u.UserMessagePreference == UserMessagePreference.Mentions)
                    .Select(u => u.Id)
                    .ToListAsync();

                filtered = filtered.Where(m =>
                {
                    return !pingOnly.Contains(m.Author.Id) || bot.DidMention(m);
                });
            }

            return filtered.ToList();
        }

        public abstract Task<string> GetResponse(SocketMessage arg, Data.Bot bot);
    }
}
