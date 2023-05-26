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
    public class GPTTextEngine : BaseEngine
    {
        private readonly OpenAIAPI _api;

        public GPTTextEngine(IServiceProvider services, OpenAIAPI api): base(services)
        {
            _api = services.GetRequiredService<OpenAIAPI>();
        }

        public override async Task<string> GetResponse(SocketMessage arg, Data.Bot bot)
        {
            var messages = await GetMessages(arg, bot);

            var chatMessages = "";

            if (bot.Prompt != "" && bot.Prompt != null)
            {
                chatMessages += bot.Prompt + "\n";
            }

            foreach (var message in messages)
            {
                var timestamp = message.Timestamp.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                chatMessages += $"[{timestamp}] <{message.Author.Username}> {message.CleanContent}";

                foreach (var attachment in message.Attachments)
                {
                    chatMessages += $"\n[attachment] {attachment.Url} {attachment.Description}";
                }

                foreach (var embed in message.Embeds)
                {
                    chatMessages += $"\n[embed] {embed.Url} {embed.Title} {embed.Description}";
                }

                chatMessages += "\n";
            }

            if (bot.PromptSuffix != "" && bot.PromptSuffix != null)
            {
                chatMessages += bot.PromptSuffix;
            }
            else
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                chatMessages += $"[{timestamp}] <{bot.Username}>";
            }

            var stopSequence = "\n[";

            if (bot.StopToken != "" && bot.StopToken != null)
            {
                stopSequence = bot.StopToken;
            }

            // string array of stop sequence and \n\n
            var stopSequences = new string[] { stopSequence, "\n\n" };

            Console.WriteLine(chatMessages);

            var response = await _api.Completions.CreateCompletionAsync(
                prompt: chatMessages,
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
