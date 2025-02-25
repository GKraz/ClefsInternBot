using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ClefsInternBot;

[RequireContext(ContextType.Guild)]
public class PingModule : InteractionModuleBase<SocketInteractionContext> {
    private static DateTime _lastPing = DateTime.MinValue;
    private static bool _dayRestrictions = true;
    private readonly double _cooldownMinutes;
    private readonly ulong _channelId;

    
    public PingModule() {
        _cooldownMinutes = double.TryParse(Environment.GetEnvironmentVariable("PING_COOLDOWN"), out var minutes)
            ? minutes
            : 60;

        _channelId = ulong.TryParse(Environment.GetEnvironmentVariable("PING_CHANNEL_ID"), out var id) 
            ? id 
            : 0;
    }


    [SlashCommand("join-notification", "Pings the discord server to join the SCP:SL server.", runMode: RunMode.Async)]
    public async Task JoinNotification() {
        await DeferAsync();

        if (_channelId == 0) {
            await FollowupAsync("Channel ID not specified in Environment!");
            return;
        }

        if (Context.Channel.Id != _channelId) {
            await FollowupAsync($"Please only use command in the <#{_channelId}> channel!");
            return;
        }
        
        var user = (SocketGuildUser)Context.User;
        var isAdmin = user.GuildPermissions.Administrator;
        
        var timeNow = DateTime.UtcNow;
        
        var restrictedDays = new[]
            { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var restrictedHours = Enumerable.Range(5, 17); // 5 - 21 (Start at 5, increment 17 times)
        var day = timeNow.DayOfWeek;
        var hour = timeNow.Hour;

        if (!isAdmin && _dayRestrictions && restrictedDays.Contains(day) && restrictedHours.Contains(hour)) {
            var lowerDateTime = new DateTime(
                timeNow.Year,
                timeNow.Month,
                timeNow.Day,
                5,
                0,
                0,
                DateTimeKind.Utc);
            var upperDateTime = new DateTime(
                timeNow.Year,
                timeNow.Month,
                timeNow.Day,
                21,
                0,
                0,
                DateTimeKind.Utc);

            var lowerUnix = ((DateTimeOffset)lowerDateTime).ToUnixTimeSeconds();
            var upperUnix = ((DateTimeOffset)upperDateTime).ToUnixTimeSeconds();
            
            await FollowupAsync($"Command can not be used between <t:{lowerUnix}:t> to <t:{upperUnix}:t> on weekdays (``{restrictedDays[0]}``-``{restrictedDays[^1]}``).");
            return;
        }
        
        if (isAdmin || timeNow > _lastPing.AddMinutes(_cooldownMinutes)) {
            await FollowupAsync($"<@{user.Id}> wants y'all to get on the server!");
            await Context.Channel.SendMessageAsync($"<@&{Environment.GetEnvironmentVariable("JOIN_ROLE_ID")}>",
                allowedMentions: AllowedMentions.All);
            
            _lastPing = timeNow;
        }
        else {
            Console.WriteLine(timeNow.Subtract(_lastPing).TotalMinutes);
            Console.WriteLine(_cooldownMinutes - timeNow.Subtract(_lastPing).TotalMinutes);
            var timeSpan = TimeSpan.FromMinutes(_cooldownMinutes - timeNow.Subtract(_lastPing).TotalMinutes);
            Console.WriteLine(timeSpan);
            await FollowupAsync($@"{timeSpan:mm\:ss} left on cooldown.");
        }
    }

    [SlashCommand("toggle-day-restrictions", "Toggles the time and day restrictions", runMode: RunMode.Async)]
    public async Task ToggleDayRestrictions() {
        await DeferAsync();
        
        var user = (SocketGuildUser)Context.User;
        var isAdmin = user.GuildPermissions.Administrator;

        if (!isAdmin) {
            await FollowupAsync("You must have administrator to use this command!");
            return;
        }
        
        _dayRestrictions = !_dayRestrictions;

        await FollowupAsync($"The day and hour restriction has been turned ``{(_dayRestrictions ? "on" : "off")}``!");
    }
}