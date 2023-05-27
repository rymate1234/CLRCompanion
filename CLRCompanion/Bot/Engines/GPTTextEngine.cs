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
        private readonly OpenAIAPI _api;

        public GPTTextEngine(IServiceProvider services, OpenAIAPI api): base(services)
        {
            _api = services.GetRequiredService<OpenAIAPI>();
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
            var stopSequences = new string[] { stopSequence, "\n\n" };

            Console.WriteLine(messages);

            var response = await _api.Completions.CreateCompletionAsync(
                prompt: messages,
                model: bot.Model,
                stopSequences: stopSequences,
                max_tokens: 1024
            );

            // filter out any initial timestamp from the response
            var msg = response.ToString();
            var filteredMsg = msg.Contains("> ") ? msg.Substring(msg.IndexOf("> ") + 2) : msg;

            return filteredMsg ?? "";
        }
    }
}
