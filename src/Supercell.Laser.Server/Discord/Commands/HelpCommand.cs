namespace Supercell.Laser.Server.Commands
{
    using NetCord.Services.Commands;
    public class Help : CommandModule<CommandContext>
    {
        [Command("help")]
        public static string HelpCommand()
        {
            return "# Available Commands:\n"
                + "!help - shows all available commands\n"
                + "!status - show server status\n"
                + "!ping - will respond with pong\n"
                + "!savecache - save players and clubs in cache\n"
                + "!unlockall - will unlock EVERYTHING on the players account (!unlockall [TAG])\n"
                + "!givepremium - gives premium to an account (!givepremium [TAG])\n"
                + "!ban - ban an account (!ban [TAG])\n"
                + "!unban - unban an account (!unban [TAG])\n"
                + "!mute - mute a player (!mute [TAG])\n"
                + "!unmute - unmute a player (!unmute [TAG])\n"
                + "!resetseason - resets the season, duh\n"
                + "!changename - changes a players name (!changename [TAG] [newName])\n"
                + "!banip - adds an ip to the blacklist (!banip [IP])\n"
                + "!unbanip - removes an ip from the blacklist (!unbanip [IP])\n"
                + "!reports - sends a link to all reported messages\n"
                + "!userinfo - show player info (!userinfo [TAG])\n"
                + "!changecredentials - change username/password of an account (!changecredentials [TAG] [newUsername] [newPassword])\n"
                + "!settrophies - set trophies of all brawlers (!settrophies [TAG] [Trophies])\n"
                + "!addgems - grant gems to a player (!addgems [TAG] [DonationCount])\n";
        }
    }
}