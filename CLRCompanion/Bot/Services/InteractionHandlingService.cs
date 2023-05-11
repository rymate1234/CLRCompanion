using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using CLRCompanion.Bot.Modules;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;

namespace CLRCompanion.Bot.Services
{
    public class InteractionHandlingService
    {
        private readonly InteractionService _service;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _provider;
        private readonly string? _server = Environment.GetEnvironmentVariable("TEST_GUILD");

        public InteractionHandlingService(IServiceProvider services)
        {
            _service = services.GetRequiredService<InteractionService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _provider = services;

            _service.Log += LogAsync;
            _client.InteractionCreated += OnInteractionAsync;
            _client.Ready += Ready;
            // For examples on how to handle post execution,
            // see the InteractionFramework samples.
        }

        private async Task Ready()
        {
            if (Program.IsDebug() && _server != null)
                await _service.RegisterCommandsToGuildAsync(ulong.Parse(_server), true);
            else
                await _service.RegisterCommandsGloballyAsync(true);
        }

        // Register all modules, and add the commands from these modules to either guild or globally depending on the build state.
        public async Task InitializeAsync()
        {
            await _service.AddModuleAsync(typeof(Commands), _provider);
        }

        private async Task OnInteractionAsync(SocketInteraction interaction)
        {
            _ = Task.Run(async () =>
            {
                var context = new SocketInteractionContext(_client, interaction);
                await _service.ExecuteCommandAsync(context, _provider);
            });
            await Task.CompletedTask;
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }
    }
}
