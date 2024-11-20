namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Server.Logic.Game;
    using Supercell.Laser.Server.Settings;

    public class Ping : CommandModule<CommandContext>
    {
        [Command("ping")]
        public static string Pong() => "Pong!";
    }

    public class Help : CommandModule<CommandContext>
    {
        [Command("help")]
        public static string help()
        {
            return
                "# Available Commands:\n" +
                "!help - shows all available commands\n" +
                "!status - show server status\n" +
                "!ping - will respond with pong\n" +
                "!ban - ban an account (!ban [TAG])\n" +
                "!unban - unban an account (!unban [TAG])\n" +
                "!mute - mute a player (!mute [TAG])\n" +
                "!unmute - unmute a player (!unmute [TAG])\n" +
                "!resetseason - resets the season, duh\n" +
                "!userinfo - show player info (!userinfo [TAG])\n";
        }
    }

    public class Ban : CommandModule<CommandContext>
    {
        [Command("ban")]
        public static string ban([CommandParameter(Remainder = true)] string playerId)
        {
            if (!playerId.StartsWith("#"))
            {
                return "Invalid player ID. Make sure it starts with '#'.";
            }

            long lowID = LogicLongCodeGenerator.ToId(playerId);
            Account account = Accounts.Load(lowID);

            if (account == null)
            {
                return $"Could not find player with ID {playerId}.";
            }

            account.Avatar.Banned = true;
            account.Avatar.Name = "Brawler";

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You have been banned!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been banned.";
        }
    }

    public class Unban : CommandModule<CommandContext>
    {
        [Command("unban")]
        public static string unban([CommandParameter(Remainder = true)] string playerId)
        {
            if (!playerId.StartsWith("#"))
            {
                return "Invalid player ID. Make sure it starts with '#'.";
            }

            long lowID = LogicLongCodeGenerator.ToId(playerId);
            Account account = Accounts.Load(lowID);

            if (account == null)
            {
                return $"Could not find player with ID {playerId}.";
            }

            account.Avatar.Banned = false;

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "Your account has been updated!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been unbanned.";
        }
    }

    public class Mute : CommandModule<CommandContext>
    {
        [Command("mute")]
        public static string mute([CommandParameter(Remainder = true)] string playerId)
        {
            if (!playerId.StartsWith("#"))
            {
                return "Invalid player ID. Make sure it starts with '#'.";
            }

            long lowID = LogicLongCodeGenerator.ToId(playerId);
            Account account = Accounts.Load(lowID);

            if (account == null)
            {
                return $"Could not find player with ID {playerId}.";
            }

            account.Avatar.IsCommunityBanned = true;
            Notification notification = new()
            {
                Id = 81,
                MessageEntry = "Social functions have been disabled for your account. If you think that an error has occurred, contact an administration."
            };
            account.Home.NotificationFactory.Add(notification);

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You've been muted!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been muted.";
        }
    }

    public class UserInfo : CommandModule<CommandContext>
    {
        [Command("userinfo")]
        public static string userInfo([CommandParameter(Remainder = true)] string playerId)
        {
            if (!playerId.StartsWith("#"))
            {
                return "Invalid player ID. Make sure it starts with '#'.";
            }

            long lowID = LogicLongCodeGenerator.ToId(playerId);
            Account account = Accounts.Load(lowID);

            if (account == null)
            {
                return $"Could not find player with ID {playerId}.";
            }

            string ipAddress = ConvertInfoToData(account.Home.IpAddress);
            string lastLoginTime = account.Home.LastVisitHomeTime.ToString();
            string device = ConvertInfoToData(account.Home.Device);
            string name = ConvertInfoToData(account.Avatar.Name);
            string token = ConvertInfoToData(account.Avatar.PassToken);
            string soloWins = ConvertInfoToData(account.Avatar.SoloWins);
            string duoWins = ConvertInfoToData(account.Avatar.DuoWins);
            string trioWins = ConvertInfoToData(account.Avatar.TrioWins);
            string trophies = ConvertInfoToData(account.Avatar.Trophies);
            string banned = ConvertInfoToData(account.Avatar.Banned);
            string muted = ConvertInfoToData(account.Avatar.IsCommunityBanned);

            return
                $"# Information of {playerId}!\n" +
                $"IpAddress: {ipAddress}\n" +
                $"Last Login Time: {lastLoginTime} UTC\n" +
                $"Device: {device}\n" +
                $"# Account Stats\n" +
                $"Name: {name}\n" +
                $"Token: {token}\n" +
                $"Trophies: {trophies}\n" +
                $"Solo Wins: {soloWins}\n" +
                $"Duo Wins: {duoWins}\n" +
                $"Trio Wins: {trioWins}\n" +
                $"Muted: {muted}\n" +
                $"Banned: {banned}";
        }

        private static string ConvertInfoToData(object data)
        {
            return data?.ToString() ?? "N/A";
        }
    }

    public class resetseason : CommandModule<CommandContext>
    {
        [Command("resetseason")]
        public static string ResetSeason()
        {

            long lastAccId2 = Accounts.GetMaxAvatarId();
            for (int accid = 1; accid <= lastAccId2; accid++)
            {
                Account thisAcc = Accounts.LoadNoChache(accid);
                if (thisAcc == null) continue;

                if (thisAcc.Avatar.Trophies >= 550)
                {
                    List<int> hhh = new();
                    List<int> ht = new();
                    List<int> htr = new();
                    List<int> sa = new();
                    int[] StarPointsTrophiesStart = { 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400 };
                    int[] StarPointsTrophiesEnd = { 599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, 1000000 };
                    int[] StarPointsSeasonRewardAmount = { 70, 120, 160, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 440, 460, 480 };
                    int[] StarPointsTrophiesInReset = { 525, 550, 600, 650, 700, 725, 750, 775, 800, 825, 850, 875, 900, 925, 950, 975, 1000, 1025 };

                    foreach (Hero h in thisAcc.Avatar.Heroes)
                    {
                        if (h.Trophies >= StarPointsTrophiesStart[0])
                        {
                            hhh.Add(h.CharacterId);
                            ht.Add(h.Trophies);
                            int i = 0;
                            while (true)
                            {
                                if (h.Trophies >= StarPointsTrophiesStart[i] && h.Trophies <= StarPointsTrophiesEnd[i])
                                {
                                    if (StarPointsTrophiesStart[i] != 1400)
                                    {
                                        htr.Add(h.Trophies - StarPointsTrophiesInReset[i]);
                                        h.Trophies = StarPointsTrophiesInReset[i];
                                        sa.Add(StarPointsSeasonRewardAmount[i]);
                                    }
                                    else
                                    {
                                        int b = h.Trophies - 1440;
                                        b /= 2;
                                        htr.Add(h.Trophies - StarPointsTrophiesInReset[i] - b);
                                        h.Trophies = (StarPointsTrophiesInReset[i] + b);
                                        sa.Add(StarPointsSeasonRewardAmount[i] + b / 2);
                                    }
                                    break;
                                }
                                else
                                {
                                    i++;
                                }
                            }
                        }
                    }

                    if (hhh.Count > 0)
                    {
                        thisAcc.Home.NotificationFactory.Add(new Notification
                        {
                            Id = 79,
                            HeroesIds = hhh,
                            HeroesTrophies = ht,
                            HeroesTrophiesReseted = htr,
                            StarpointsAwarded = sa,
                        });
                    }
                }

                Accounts.Save(thisAcc);
                Console.WriteLine(accid);
            }

            return "Season reset completed for all players.";
        }
    }

    public class Status : CommandModule<CommandContext>
    {
        [Command("status")]
        public static string status()
        {
            long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
            DateTime startTime = Process.GetCurrentProcess().StartTime;
            DateTime now = DateTime.Now;

            TimeSpan uptime = now - startTime;

            string formattedUptime = string.Format("{0}{1}{2}{3}",
                uptime.Days > 0 ? $"{uptime.Days} Days, " : string.Empty,
                uptime.Hours > 0 || uptime.Days > 0 ? $"{uptime.Hours} Hours, " : string.Empty,
                uptime.Minutes > 0 || uptime.Hours > 0 ? $"{uptime.Minutes} Minutes, " : string.Empty,
                uptime.Seconds > 0 ? $"{uptime.Seconds} Seconds" : string.Empty);

            return
                "# Server Status\n" +
                $"Server Game Version: v29.231\n" +
                $"Server Build: v1.0 from 10.02.2024\n" +
                $"Resources Sha: {Fingerprint.Sha}\n" +
                $"Environment: Prod\n" +
                $"Server Time: {now} UTC\n" +
                $"Players Online: {Sessions.Count}\n" +
                $"Memory Used: {megabytesUsed} MB\n" +
                $"Uptime: {formattedUptime}\n" +
                $"Accounts Cached: {AccountCache.Count}\n" +
                $"Alliances Cached: {AllianceCache.Count}\n" +
                $"Teams Cached: {Teams.Count}\n";
        }
    }
}