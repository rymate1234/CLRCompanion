using Azure.AI.OpenAI;
using CLRCompanion.Bot.Services;
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
    public class GPTTextEngine : TextEngine
    {
        private readonly OpenAIService _api;

        public GPTTextEngine(IServiceProvider services, OpenAIService api): base(services)
        {
            _api = services.GetRequiredService<OpenAIService>();
        }

        public override async Task<string> GetResponse(SocketMessage arg, Data.Bot bot)
        {
            var messages = await GetTextMessages(arg, bot);

            var stopSequence = "\n[";

            if (bot.StopToken != "" && bot.StopToken != null)
            {
                stopSequence = bot.StopToken;
            }

            // string array of stop sequence and \n\n
            Console.WriteLine(messages);

            var response = await _api.GetCompletionsAsync(
                bot.Model,
                new CompletionsOptions()
                {
                    Prompts = { messages },
                    StopSequences = { stopSequence, "\n\n" },
                    MaxTokens = 1024
                }
            );

            // filter out any initial timestamp from the response
            var msg = response.Value.Choices[0].Text;
            var filteredMsg = msg.Contains("> ") ? msg.Substring(msg.IndexOf("> ") + 2) : msg;

            return filteredMsg ?? "";
        }
    }
}
