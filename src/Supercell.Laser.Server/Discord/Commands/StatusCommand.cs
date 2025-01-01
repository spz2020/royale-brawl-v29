namespace Supercell.Laser.Server.Discord.Commands
{
    using System.Diagnostics;
    using NetCord.Services.Commands;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Logic.Game;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Server.Settings;
    public class Status : CommandModule<CommandContext>
    {
        [Command("status")]
        public static string status()
        {
            long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
            DateTime startTime = Process.GetCurrentProcess().StartTime;
            DateTime now = DateTime.Now;

            TimeSpan uptime = now - startTime;

            string formattedUptime = string.Format(
                "{0}{1}{2}{3}",
                uptime.Days > 0 ? $"{uptime.Days} Days, " : string.Empty,
                uptime.Hours > 0 || uptime.Days > 0 ? $"{uptime.Hours} Hours, " : string.Empty,
                uptime.Minutes > 0 || uptime.Hours > 0
                  ? $"{uptime.Minutes} Minutes, "
                  : string.Empty,
                uptime.Seconds > 0 ? $"{uptime.Seconds} Seconds" : string.Empty
            );

            return "# Server Status\n"
                + $"Server Game Version: v29.258\n"
                + $"Server Build: v1.0 from 10.02.2024\n"
                + $"Resources Sha: {Fingerprint.Sha}\n"
                + $"Environment: Prod\n"
                + $"Server Time: {now} UTC\n"
                + $"Players Online: {Sessions.Count}\n"
                + $"Memory Used: {megabytesUsed} MB\n"
                + $"Uptime: {formattedUptime}\n"
                + $"Accounts Cached: {AccountCache.Count}\n"
                + $"Alliances Cached: {AllianceCache.Count}\n"
                + $"Teams Cached: {Teams.Count}\n";
        }
    }
}