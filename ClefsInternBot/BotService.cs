using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClefsInternBot;

public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotService> _logger;


    public BotService(DiscordSocketClient client, InteractionService interactionService, IServiceProvider serviceProvider, ILogger<BotService> logger) {
        _client = client;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += ClientReady;
        _client.InteractionCreated += OnInteraction;

        Console.WriteLine(Environment.GetEnvironmentVariable("TOKEN"));

        await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= ClientReady;
        _client.InteractionCreated -= OnInteraction;

        await _client.StopAsync();
    }

    private async Task ClientReady() {
        _logger.LogInformation("Discord Client Ready!");

        await RegisterCommands();
    }

    private async Task RegisterCommands() {
        _logger.LogInformation("Registering commands...");

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        
        var errorCount = 0;

        foreach (var guild in _client.Guilds) {
            if (guild == null) continue;

            try {
                await _interactionService.RegisterCommandsToGuildAsync(guild.Id);
            }
            catch (Exception e) {
                errorCount++;
                _logger.LogError("{Message}", $"An error occured while registering commands to {guild.Name} ({guild.Id}):\n{e.Message}");
            }
        }

        _logger.LogInformation("{Message}", $"Commands registered to {_client.Guilds.Count} server(s) with {errorCount} error(s).");
    }

    private Task<IResult> OnInteraction(SocketInteraction interaction) {
        var interactionContext = new SocketInteractionContext(_client, interaction);
        return _interactionService.ExecuteCommandAsync(interactionContext, _serviceProvider);
    }
}