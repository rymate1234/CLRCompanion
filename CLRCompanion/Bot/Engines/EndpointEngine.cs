using CLRCompanion.Data;
using CLRCompanion.Pages;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System;

namespace CLRCompanion.Bot.Engines
{
    public class EndpointEngine : TextEngine
    {
        static readonly HttpClient client = new HttpClient();

        public EndpointEngine(IServiceProvider services): base(services)
        {
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

            var data = new { text = messages };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send a POST request
            var response = await client.PostAsync(bot.Model, content);

            // Get the response content
            var filteredMsg = await response.Content.ReadAsStringAsync();

            // end the filtered message at the first of the stop sequences
            if (filteredMsg.Contains(stopSequences[0]))
            {
                filteredMsg = filteredMsg.Substring(0, filteredMsg.IndexOf(stopSequences[0]));
            }
            else if (filteredMsg.Contains(stopSequences[1]))
            {
                filteredMsg = filteredMsg.Substring(0, filteredMsg.IndexOf(stopSequences[1]));
            }

            filteredMsg = filteredMsg.Contains("> ") ? filteredMsg.Substring(filteredMsg.IndexOf("> ") + 2) : filteredMsg;

            return filteredMsg ?? "";
        }
    }
}
