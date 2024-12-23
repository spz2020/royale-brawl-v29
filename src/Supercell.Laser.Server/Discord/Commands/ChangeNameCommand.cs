namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;

    public class ChangeNameCommand : CommandModule<CommandContext>
    {
        [Command("changename")]
        public static string ChangeName([CommandParameter(Remainder = true)] string playerIdAndName)
        {
            string[] parts = playerIdAndName.Split(' ');
            if (
                parts.Length < 2
                || !parts[0].StartsWith("#")
            )
            {
                return "Usage: !changename [TAG] [NewName]";
            }

            string playerId = parts[0];
            string newName = string.Join(" ", parts.Skip(1));

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

            if (string.IsNullOrWhiteSpace(newName))
            {
                return "The new name cannot be empty or contain only whitespace.";
            }

            account.Avatar.Name = newName;

            if (Sessions.IsSessionActive(lowID))
            {
                Session session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage()
                    {
                        Message =
                            $"Your name has been changed!"
                    }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has had their name changed to {newName}.";
        }
    }
}