using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ClefsInternBot;

[RequireContext(ContextType.Guild)]
public class PingModule : InteractionModuleBase<SocketInteractionContext> {
    private static DateTime _lastPing = DateTime.MinValue;
    private readonly double _cooldownMinutes;

    
    public PingModule() {
        if (double.TryParse(Environment.GetEnvironmentVariable("PING_COOLDOWN"), out var minutes)) {
            _cooldownMinutes = minutes;
        }
        else {
            _cooldownMinutes = 60;
        }
    }
    
    
    [SlashCommand("join-notification", "Pings the discord server to join the SCP:SL server.", runMode: RunMode.Async)]
    public async Task JoinNotification() {
        await DeferAsync();
        
        var isAdmin = ((SocketGuildUser)Context.User).GuildPermissions.Administrator;
        if (isAdmin || DateTime.Now > _lastPing.AddMinutes(_cooldownMinutes)) {
            await FollowupAsync($"<@&{Environment.GetEnvironmentVariable("JOIN_ROLE_ID")}>", allowedMentions: AllowedMentions.All);
            _lastPing = DateTime.Now;
        }
        else {
            await FollowupAsync($"{_cooldownMinutes - DateTime.Now.Subtract(_lastPing).TotalMinutes:F} minute(s) left on cooldown.", ephemeral: true);
        }
    }
}