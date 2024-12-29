namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Utils;
    public class UserInfo : CommandModule<CommandContext>
    {
        [Command("userinfo")]
        public static string UserInfoCommand([CommandParameter(Remainder = true)] string playerId)
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
            string premium = ConvertInfoToData(account.Avatar.IsPremium);
            string username = DatabaseHelper.ExecuteScalar(
                "SELECT username FROM users WHERE id = @id",
                ("@id", lowID)
            );
            string password = DatabaseHelper.ExecuteScalar(
                "SELECT password FROM users WHERE id = @id",
                ("@id", lowID)
            );

            return $"# Information of {playerId}!\n"
                + $"IpAddress: {ipAddress}\n"
                + $"Last Login Time: {lastLoginTime} UTC\n"
                + $"Device: {device}\n"
                + $"# Account Stats\n"
                + $"Name: {name}\n"
                + $"Token: {token}\n"
                + $"Trophies: {trophies}\n"
                + $"Solo Wins: {soloWins}\n"
                + $"Duo Wins: {duoWins}\n"
                + $"Trio Wins: {trioWins}\n"
                + $"Premium: {premium}\n"
                + $"Muted: {muted}\n"
                + $"Banned: {banned}\n"
                + $"# royale ID\n"
                + $"Username: {username}\n"
                + $"Password: {password}";
        }

        private static string ConvertInfoToData(object data)
        {
            return data?.ToString() ?? "N/A";
        }
    }
}