namespace Supercell.Laser.Server.Discord.Commands
{
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    public class ResetSeason : CommandModule<CommandContext>
    {
        [Command("resetseason")]
        public static string ResetSeasonCommand()
        {
            long maxAccountId = Accounts.GetMaxAvatarId();

            for (int accountId = 1; accountId <= maxAccountId; accountId++)
            {
                Account account = Accounts.LoadNoCache(accountId);
                if (account == null)
                    continue;

                if (account.Avatar.Trophies >= 550)
                {
                    List<int> heroIds = new();
                    List<int> heroTrophies = new();
                    List<int> resetTrophies = new();
                    List<int> starPointsAwarded = new();

                    int[] trophyRangesStart =
                    {
                        550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400
                    };

                    int[] trophyRangesEnd =
                    {
                        599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, 1000000
                    };

                    int[] seasonRewardAmounts =
                    {
                        70, 120, 160, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 440, 460, 480
                    };

                    int[] trophyResetValues =
                    {
                        525, 550, 600, 650, 700, 725, 750, 775, 800, 825, 850, 875, 900, 925, 950, 975, 1000, 1025
                    };

                    foreach (Hero hero in account.Avatar.Heroes)
                    {
                        if (hero.Trophies >= trophyRangesStart[0])
                        {
                            heroIds.Add(hero.CharacterId);
                            heroTrophies.Add(hero.Trophies);

                            int index = 0;
                            while (true)
                            {
                                if (hero.Trophies >= trophyRangesStart[index] && hero.Trophies <= trophyRangesEnd[index])
                                {
                                    if (trophyRangesStart[index] != 1400)
                                    {
                                        int trophiesReset = hero.Trophies - trophyResetValues[index];
                                        hero.Trophies = trophyResetValues[index];
                                        resetTrophies.Add(trophiesReset);
                                        starPointsAwarded.Add(seasonRewardAmounts[index]);
                                    }
                                    else
                                    {
                                        int extraTrophies = hero.Trophies - 1440;
                                        extraTrophies /= 2;
                                        int trophiesReset = hero.Trophies - trophyResetValues[index] - extraTrophies;
                                        hero.Trophies = trophyResetValues[index] + extraTrophies;
                                        starPointsAwarded.Add(seasonRewardAmounts[index] + (extraTrophies / 2));
                                        resetTrophies.Add(trophiesReset);
                                    }
                                    break;
                                }
                                index++;
                            }
                        }
                    }

                    if (heroIds.Count > 0)
                    {
                        account.Home.NotificationFactory.Add(
                            new Notification
                            {
                                Id = 79,
                                HeroesIds = heroIds,
                                HeroesTrophies = heroTrophies,
                                HeroesTrophiesReseted = resetTrophies,
                                StarpointsAwarded = starPointsAwarded,
                            }
                        );
                    }
                }
                Accounts.Save(account);
            }

            return "Season reset completed for all players.";
        }
    }
}