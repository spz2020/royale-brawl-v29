namespace Supercell.Laser.Server.Discord.Commands
{
    using System.Threading.Tasks;
    using NetCord.Services.Commands;
    using Supercell.Laser.Server.Database.Cache;

    public class SaveCache : CommandModule<CommandContext>
    {
        [Command("savecache")]
        public async Task<string> SaveCache()
        {
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            return "all players and clubs in cache saved to database.";
        }
    }
}