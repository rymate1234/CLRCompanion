using CLRCompanion.Bot.Services;
using CLRCompanion.Data;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenAI_API;
using System.Threading.Channels;

namespace CLRCompanion.Bot
{
    public class DiscordBot
    {
        private readonly string? _token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                })
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<InteractionHandlingService>()
                .AddSingleton<MessageService>()
                .AddSingleton<OpenAIAPI>()
                .AddDbContext<ApplicationDbContext>
                (
                    options => options.UseSqlite("Data Source=app.db")
                )
                .BuildServiceProvider();
        }

        public DiscordBot()
        {
            if (_token == null)
            {
                Console.WriteLine("No token found.");
                // exit the program
                Environment.Exit(1);
            }
        }

        // initialise a discord bot
        public async void StartAsync()
        {
            using var services = ConfigureServices();

            var client = services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            services.GetRequiredService<InteractionService>().Log += LogAsync;

            // Tokens should be considered secret data and never hard-coded.
            // We can read from the environment variable to avoid hard coding.
            await client.LoginAsync(TokenType.Bot, _token);
            await client.StartAsync();

            // Here we initialize the logic required to register our commands.
            await services.GetRequiredService<InteractionHandlingService>().InitializeAsync();
            services.GetRequiredService<MessageService>().Initialize();

            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
