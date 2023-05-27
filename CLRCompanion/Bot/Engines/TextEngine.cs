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
    public abstract class TextEngine : BaseEngine
    {
        protected TextEngine(IServiceProvider services) : base(services)
        {
        }

        internal async Task<string> GetTextMessages(SocketMessage arg, Data.Bot bot)
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

            return chatMessages;
        }
    }
}
