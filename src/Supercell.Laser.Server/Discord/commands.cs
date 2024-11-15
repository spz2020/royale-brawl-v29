using NetCord.Services.Commands;

namespace Supercell.Laser.Server.Discord.commands
{
    public class ping : CommandModule<CommandContext>
    {
        [Command("pong")]
        public static string Pong() => "Ping!";
    }
    public class ping1 : CommandModule<CommandContext>
    {
        [Command("pong1")]
        public static string Pong() => "Ping1!";
    }
}
