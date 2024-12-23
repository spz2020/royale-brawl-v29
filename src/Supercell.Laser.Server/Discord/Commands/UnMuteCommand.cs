namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;
    public class UnMute : CommandModule<CommandContext>
    {
        [Command("unmute")]
        public static string UnmuteCommand([CommandParameter(Remainder = true)] string playerId)
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

            account.Avatar.IsCommunityBanned = false;
            Notification notification = new() { Id = 81, MessageEntry = "You have been unmuted, you can now chat again." };
            account.Home.NotificationFactory.Add(notification);

            if (Sessions.IsSessionActive(lowID))
            {
                Session session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You've been unmuted!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been unmuted.";
        }
    }
}