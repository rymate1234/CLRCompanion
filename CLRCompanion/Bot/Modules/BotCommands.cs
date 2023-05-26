using Discord.Commands;
using Discord;
using CLRCompanion.Data;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace CLRCompanion.Bot.Modules
{
    public class BotAutocompleteHandler : AutocompleteHandler
    {
        public ApplicationDbContext DbContext { get; set; }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var results = DbContext.Bots
                .Where(b => b.ChannelId == context.Channel.Id)
                .Take(25)
                .Select(b => new AutocompleteResult
                {
                    Name = b.Username,
                    Value = b.Id.ToString()
                })
                .ToList();

            return AutocompletionResult.FromSuccess(results);
        }
    }


    public class BotCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public ApplicationDbContext DbContext { get; set; }

        [SlashCommand("add", "Adds a bot to the database.")]
        public async Task AddAsync
        (
            string username,
            string prompt = "When spoken to, it will always deny the users request and tell the user they should configure the prompt",
            string model = "gpt-3.5-turbo",
            ModelType type = ModelType.GPTChat,
            double chance = 0.01,
            int limit = 5,
            string? avatarUrl = null,
            bool defaultBot = false,
            bool ignorePings = false,
            string? stopToken = null,
            string? promptSuffix = null
        )
        {
            await RespondAsync("Adding bot...", ephemeral: true);
            var bot = new Data.Bot
            {
                ChannelId = Context.Channel.Id,
                Username = username,
                Prompt = prompt,
                Model = model,
                ModelType = type,
                Chance = chance,
                Limit = limit,
                AvatarUrl = avatarUrl,
                Default = defaultBot,
                IgnorePings = ignorePings,
                StopToken = stopToken,
                PromptSuffix = promptSuffix
            };
            DbContext.Bots.Add(bot);
            await DbContext.SaveChangesAsync();
            await FollowupAsync($"Added bot {username} to the database.", ephemeral: true);
        }

        [SlashCommand("list", "List all bots in the channel.")]
        public async Task ListAsync()
        {
            var bots = await DbContext.Bots.Where(b => b.ChannelId == Context.Channel.Id).ToListAsync();
            var embed = new EmbedBuilder();
            embed.WithTitle("Bots");
            foreach (var bot in bots)
            {
                var prompt = bot.TruncatedPrompt;
                embed.AddField($"@{bot.Username} (`{bot.Id}`)", $"Prompt: {prompt}\nModel: {bot.Model}\nChance: {bot.Chance}\nLimit: {bot.Limit}");
            }
            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("config", "Gets the config for a single bot.")]
        public async Task ConfigAsync([Autocomplete(typeof(BotAutocompleteHandler))] string bot)
        {
            var botId = int.Parse(bot);
            var botEntity = await DbContext.Bots.FirstAsync(b => b.Id == botId);
            var embed = new EmbedBuilder();
            embed.WithTitle($"@{botEntity.Username} (`{botEntity.Id}`)");
            embed.AddField("Model", botEntity.Model);
            embed.AddField("Model Type", botEntity.ModelType);
            embed.AddField("Prompt", botEntity.TruncatedPrompt);
            embed.AddField("Chance", botEntity.Chance);
            embed.AddField("Limit", botEntity.Limit);
            embed.AddField("Avatar URL", botEntity.AvatarUrl ?? "Not Set");
            embed.AddField("Default", botEntity.Default);
            embed.AddField("Ignore Pings", botEntity.IgnorePings);
            embed.AddField("Stop Token", botEntity.StopToken ?? "Not Set");
            embed.AddField("Prompt Suffix", botEntity.PromptSuffix ?? "Not Set");
            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("prompt", "Gets the prompt for a single bot.")]
        public async Task PromptAsync([Autocomplete(typeof(BotAutocompleteHandler))] string bot)
        {
            var botId = int.Parse(bot);
            var botEntity = await DbContext.Bots.FirstAsync(b => b.Id == botId);
            await RespondAsync($"**{botEntity.Username}**: {botEntity.Prompt}", ephemeral: true);
        }

        [SlashCommand("delete", "Deletes a bot from the database.")]
        public async Task DeleteAsync([Autocomplete(typeof(BotAutocompleteHandler))] string bot)
        {
            var botId = int.Parse(bot);
            var botEntity = await DbContext.Bots.FirstAsync(b => b.Id == botId);
            DbContext.Remove(botEntity);
            await DbContext.SaveChangesAsync();
            await RespondAsync($"Deleted bot {botEntity.Username} from the database.", ephemeral: true);
            await FollowupAsync($"The prompt was: {botEntity.Prompt}", ephemeral: true);
        }

        [SlashCommand("edit", "Edits a bot from the database.")]
        public async Task EditAsync
        (
            [Autocomplete(typeof(BotAutocompleteHandler))] string bot,
            string? username = null,
            string? prompt = null,
            string? model = null,
            ModelType? type = null,
            double? chance = null,
            int? limit = null,
            string? avatarUrl = null,
            bool? defaultBot = null,
            bool? ignorePings = null,
            string? stopToken = null,
            string? promptSuffix = null
        )
        {
            var botId = int.Parse(bot);
            await RespondAsync("Editing bot...", ephemeral: true);
            var botEntity = await DbContext.Bots.FirstAsync(b => b.Id == botId);

            if (username != null)
            {
                botEntity.Username = username;
            }

            if (prompt != null)
            {
                botEntity.Prompt = prompt;
            }

            if (model != null)
            {
                botEntity.Model = model;
            }

            if (type != null)
            {
                botEntity.ModelType = type.Value;
            }

            if (chance != null)
            {
                botEntity.Chance = chance.Value;
            }

            if (limit != null)
            {
                botEntity.Limit = limit.Value;
            }

            if (avatarUrl != null)
            {
                botEntity.AvatarUrl = avatarUrl;
            }

            if (defaultBot != null)
            {
                botEntity.Default = defaultBot.Value;
            }

            if (ignorePings != null)
            {
                botEntity.IgnorePings = ignorePings.Value;
            }

            if (stopToken != null)
            {
                botEntity.StopToken = stopToken;
            }

            if (promptSuffix != null)
            {
                botEntity.PromptSuffix = promptSuffix;
            }

            await DbContext.SaveChangesAsync();

            await FollowupAsync($"Edited bot {botEntity.Username}.", ephemeral: true);
        }

        [SlashCommand("ask", "Ask a bot a question.")]
        public async Task AskAsync
        (
            [Autocomplete(typeof(BotAutocompleteHandler))] string bot,
            string question
        )
        {
            var botId = int.Parse(bot);
            var botEntity = await DbContext.Bots.FirstAsync(b => b.Id == botId);
            await RespondAsync($"@{botEntity.Username} {question}");
        }
    }
}
