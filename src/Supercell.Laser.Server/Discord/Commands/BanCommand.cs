namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;

    public class Ban : CommandModule<CommandContext>
    {
        [Command("ban")]
        public static string BanCommand([CommandParameter(Remainder = true)] string playerId)
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
            account.Avatar.ResetTrophies();

            if (Sessions.IsSessionActive(lowID))
            {
                Session session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You have been banned!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been banned.";
        }
    }
}