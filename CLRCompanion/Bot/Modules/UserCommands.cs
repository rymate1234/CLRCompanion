using Discord.Commands;
using Discord;
using CLRCompanion.Data;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace CLRCompanion.Bot.Modules
{
    public class UserCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public ApplicationDbContext DbContext { get; set; }
        [SlashCommand("ping", "Replies with pong!")]
        public Task PingAsync() => RespondAsync("pong!");

        [SlashCommand("preferences", "Edits your preferences.")]
        public async Task AddAsync
        (
            UserMessagePreference userMessage = UserMessagePreference.All,
            bool dontPing = false
        )
        {
            await RespondAsync("Editing preferences...", ephemeral: true);

            // get user id
            var userId = Context.User.Id;
            var user = await DbContext.UserPreferences.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                user = new UserPreferences
                {
                    Id = userId,
                    UserMessagePreference = userMessage,
                    DontPing = dontPing
                };
                DbContext.UserPreferences.Add(user);
            }
            else
            {
                user.UserMessagePreference = userMessage;
                user.DontPing = dontPing;
            }
            await DbContext.SaveChangesAsync();

            await FollowupAsync($"Edited your preferences to {userMessage}", ephemeral: true);
        }

        [SlashCommand("me", "Gets your preferences.")]
        public async Task ConfigAsync()
        {
            // get user id
            var userId = Context.User.Id;
            var user = await DbContext.UserPreferences.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                await RespondAsync("You have no preferences set.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithTitle($"@{Context.User.Username} (`{userId}`)");
            embed.AddField("What messages can the bot see?", user.UserMessagePreference);
            embed.AddField("Should the bot ping you?", user.DontPing ? "No" : "Yes");
            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
    }
}
