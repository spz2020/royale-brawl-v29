namespace Supercell.Laser.Server.Discord.Commands
{
    using System.Net;
    using NetCord.Services.Commands;
    using Supercell.Laser.Server.Settings;
    public class UnbanIP : CommandModule<CommandContext>
    {
        [Command("unbanip")]
        public static string UnbanIpCommand([CommandParameter(Remainder = true)] string ipAddress)
        {
            if (!Configuration.Instance.antiddos)
            {
                return "Anti-DDoS is disabled. Enable it in config.json to use this command.";
            }

            if (!IPAddress.TryParse(ipAddress, out _))
            {
                return "Invalid IP address format.";
            }

            if (!IsIpBanned(ipAddress))
            {
                return $"IP address {ipAddress} is not banned.";
            }

            try
            {
                string[] bannedIps = File.ReadAllLines("ipblacklist.txt");
                bannedIps = bannedIps.Where(ip => ip != ipAddress).ToArray();
                File.WriteAllLines("ipblacklist.txt", bannedIps);
                return $"IP address {ipAddress} has been unbanned.";
            }
            catch (Exception ex)
            {
                return $"Failed to unban IP address: {ex.Message}";
            }
        }

        private static bool IsIpBanned(string ipAddress)
        {
            if (!File.Exists("ipblacklist.txt"))
            {
                return false;
            }

            string[] bannedIps = File.ReadAllLines("ipblacklist.txt");
            return bannedIps.Contains(ipAddress);
        }
    }
}