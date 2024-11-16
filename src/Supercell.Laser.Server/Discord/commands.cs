namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Home.Items;

    public class ping : CommandModule<CommandContext>
    {
        [Command("ping")]
        public static string Pong() => "Pong!";
    }
    public class ban : CommandModule<CommandContext>
    {
        [Command("ban")]
        public static string Ban([CommandParameter(Remainder = true)] string playerId)
        {
            if (!playerId.StartsWith("#"))
            {
                return "Invalid player ID. Make sure it starts with '#'.";
            }
            long lowID = LogicLongCodeGenerator.ToId(playerId);
            Account accountban = Accounts.Load(lowID);
            if (accountban == null)
            {
                return $"Could not find player with ID {playerId}.";
            }
            accountban.Avatar.Banned = true;
            accountban.Avatar.Name = "Brawler";
            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage() { Message = "you have been banned!" }
                );
                Sessions.Remove(lowID);
            }
            return $"Player with ID {playerId} has been banned.";
        }
    }

    public class unban : CommandModule<CommandContext>
    {
        [Command("unban")]
        public static string Unban([CommandParameter(Remainder = true)] string playerId)
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
                    new AuthenticationFailedMessage() { Message = "Your account updated!" }
                );
                Sessions.Remove(lowID);
            }
            return $"Player with ID {playerId} has been unbanned.";
        }
    }

    public class mute : CommandModule<CommandContext>
    {
        [Command("mute")]
        public static string Mute([CommandParameter(Remainder = true)] string playerId)
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
            Notification bdn =
                new()
                {
                    Id = 81,
                    MessageEntry =
                        "Social functions have been disabled for your account\n if you think that an error has occurred, contact an administration"
                };
            account.Home.NotificationFactory.Add(bdn);
            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage() { Message = "you've been muted!" }
                );
                Sessions.Remove(lowID);
            }
            return $"Player with ID {playerId} has been muted.";
        }
    }

    public class userinfo : CommandModule<CommandContext>
    {
        [Command("userinfo")]
        public static string UserInfo([CommandParameter(Remainder = true)] string playerId)
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
            
            string ipaddress = ConvertInfoToData(account.Home.IpAddress);
            string lastlogintime = account.Home.LastVisitHomeTime.ToString();
            string device = ConvertInfoToData(account.Home.Device);
            string name = ConvertInfoToData(account.Avatar.Name);
            string token = ConvertInfoToData(account.Avatar.PassToken);
            string solowins = ConvertInfoToData(account.Avatar.SoloWins);
            string duowins = ConvertInfoToData(account.Avatar.DuoWins);
            string triowins = ConvertInfoToData(account.Avatar.TrioWins);
            string trophies = ConvertInfoToData(account.Avatar.Trophies);
            string banned = ConvertInfoToData(account.Avatar.Banned);
            string muted = ConvertInfoToData(account.Avatar.IsCommunityBanned);

            string response =
                $"# Information of {playerId}!\n"
                + $"IpAddress: {ipaddress}\n"
                + $"Last Login Time: {lastlogintime} UTC\n"
                + $"Device: {device}\n"
                + $"# account stats\n"
                + $"Name: {name}\n"
                + $"Token: {token}\n"
                + $"Trophies: {trophies}\n"
                + $"Solo wins: {solowins}\n"
                + $"Duo wins: {duowins}\n"
                + $"Trio wins: {triowins}\n"
                + $"Muted: {muted}\n"
                + $"Banned: {banned}";

            return response;
        }

        private static string ConvertInfoToData(object data)
        {
            return data?.ToString() ?? "N/A";
        }
    }
}
