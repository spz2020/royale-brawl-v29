namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;
    public class AddGems : CommandModule<CommandContext>
    {
        [Command("addgems")]
        public static string addgems([CommandParameter(Remainder = true)] string playerIdAndAmount)
        {
            string[] parts = playerIdAndAmount.Split(' ');
            if (
                parts.Length != 2
                || !parts[0].StartsWith("#")
                || !int.TryParse(parts[1], out int donationAmount)
            )
            {
                return "Usage: !addgems [TAG] [DonationCount]";
            }

            long lowID = LogicLongCodeGenerator.ToId(parts[0]);
            Account account = Accounts.Load(lowID);

            if (account == null)
            {
                return $"Could not find player with ID {parts[0]}.";
            }

            Notification nGems = new()
            {
                Id = 89,
                DonationCount = donationAmount,
                MessageEntry = $"<c6>received {donationAmount} gems, enjoy!</c>"
            };
            account.Home.NotificationFactory.Add(nGems);
            LogicAddNotificationCommand acmGems = new() { Notification = nGems };
            AvailableServerCommandMessage asmGems = new();
            asmGems.Command = acmGems;

            if (Sessions.IsSessionActive(lowID))
            {
                Session sessionGems = Sessions.GetSession(lowID);
                sessionGems.GameListener.SendTCPMessage(asmGems);
            }

            return $"Granted {donationAmount} gems to player with ID {parts[0]}.";
        }
    }
}