using ClefsInternBot;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices((context, services) => {
    services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig {
        GatewayIntents = GatewayIntents.AllUnprivileged
    }));

    services.AddSingleton(provider => {
        var client = provider.GetRequiredService<DiscordSocketClient>();
        return new InteractionService(client, new InteractionServiceConfig());
    });

    services.AddHostedService<BotService>();
});

var host = hostBuilder.Build();

await host.RunAsync();