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
    public class GivePremium : CommandModule<CommandContext>
    {
        [Command("givepremium")]
        public static string GivePremiumCommand(
            [CommandParameter(Remainder = true)] string parameters
        )
        {
            string[] parts = parameters.Split(' ');
            if (parts.Length != 2)
            {
                return "Usage: !givepremium [TAG] [DURATION_IN_MONTHS]";
            }

            long id = 0;
            bool sc = false;

            if (parts[0].StartsWith('#'))
            {
                id = LogicLongCodeGenerator.ToId(parts[0]);
            }
            else
            {
                sc = true;
                if (!long.TryParse(parts[0], out id))
                {
                    return "Invalid player ID format.";
                }
            }

            if (!int.TryParse(parts[1], out int durationMonths) || durationMonths <= 0)
            {
                return "Invalid duration. Please provide a positive number of months.";
            }

            Account account = Accounts.Load(id);
            if (account == null)
            {
                return $"Could not find player with ID {parts[0]}.";
            }

            if (account.Home.PremiumEndTime < DateTime.UtcNow)
            {
                account.Home.PremiumEndTime = DateTime.UtcNow.AddMonths(durationMonths);
            }
            else
            {
                account.Home.PremiumEndTime = account.Home.PremiumEndTime.AddMonths(durationMonths);
            }

            account.Avatar.PremiumLevel = 1;

            string formattedDate = account.Home.PremiumEndTime.ToString("dd'th of' MMMM yyyy");

            Notification n = new()
            {
                Id = 89,
                DonationCount = 40,
                MessageEntry =
                    $"<c6>Vip status activated/extended to {account.Home.PremiumEndTime} UTC! ({formattedDate})</c>"
            };

            account.Home.NotificationFactory.Add(n);

            LogicAddNotificationCommand acm = new() { Notification = n };

            AvailableServerCommandMessage asm = new() { Command = acm };

            if (Sessions.IsSessionActive(id))
            {
                Session session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(asm);
            }

            string d = sc ? LogicLongCodeGenerator.ToCode(id) : parts[0];
            return $"Done: set vip status for {d} activated/extended to {account.Home.PremiumEndTime} UTC! ({formattedDate})";
        }
    }
}