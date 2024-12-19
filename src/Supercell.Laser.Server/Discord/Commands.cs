namespace Supercell.Laser.Server.Discord
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NetCord.Services.Commands;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Logic.Game;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Server.Settings;
    using MySql.Data.MySqlClient;

    public class DatabaseHelper
    {
        private static string GetConnectionString()
        {
            return $"server=127.0.0.1;"
                + $"user={Configuration.Instance.DatabaseUsername};"
                + $"database={Configuration.Instance.DatabaseName};"
                + $"port=3306;"
                + $"password={Configuration.Instance.DatabasePassword}";
        }

        public static string ExecuteScalar(string query, params (string, object)[] parameters)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    foreach (var (paramName, paramValue) in parameters)
                    {
                        cmd.Parameters.AddWithValue(paramName, paramValue);
                    }

                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "N/A";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static bool ExecuteNonQuery(string query, params (string, object)[] parameters)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    foreach (var (paramName, paramValue) in parameters)
                    {
                        cmd.Parameters.AddWithValue(paramName, paramValue);
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
    }

    public class Ping : CommandModule<CommandContext>
    {
        [Command("ping")]
        public static string Pong() => "Pong!";
    }

    public class Help : CommandModule<CommandContext>
    {
        [Command("help")]
        public static string help()
        {
            return "# Available Commands:\n"
                + "!help - shows all available commands\n"
                + "!status - show server status\n"
                + "!ping - will respond with pong\n"
                + "!unlockall - will unlock EVERYTHING on the players account (!unlockall [TAG])\n"
                + "!givepremium - gives premium to an account (!givepremium [TAG])\n"
                + "!ban - ban an account (!ban [TAG])\n"
                + "!unban - unban an account (!unban [TAG])\n"
                + "!mute - mute a player (!mute [TAG])\n"
                + "!unmute - unmute a player (!unmute [TAG])\n"
                + "!resetseason - resets the season, duh\n"
                + "!reports - sends a link to all reported messages\n"
                + "!userinfo - show player info (!userinfo [TAG])\n"
                + "!changecredentials - change username/password of an account (!changecredentials [TAG] [newUsername] [newPassword])\n"
                + "!settrophies - set trophies of all brawlers (!settrophies [TAG] [Trophies])\n"
                + "!addgems - grant gems to a player (!addgems [TAG] [DonationCount])\n";
        }
    }

    public class Ban : CommandModule<CommandContext>
    {
        [Command("ban")]
        public static string ban([CommandParameter(Remainder = true)] string playerId)
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

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You have been banned!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been banned.";
        }
    }

    public class Unban : CommandModule<CommandContext>
    {
        [Command("unban")]
        public static string unban([CommandParameter(Remainder = true)] string playerId)
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

            account.Avatar.Banned = false;

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "Your account has been updated!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been unbanned.";
        }
    }

    public class Mute : CommandModule<CommandContext>
    {
        [Command("mute")]
        public static string mute([CommandParameter(Remainder = true)] string playerId)
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

            account.Avatar.IsCommunityBanned = true;
            Notification notification =
                new()
                {
                    Id = 81,
                    MessageEntry =
                        "Social functions have been disabled for your account. If you think that an error has occurred, contact an administration."
                };
            account.Home.NotificationFactory.Add(notification);

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You've been muted!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been muted.";
        }
    }

    public class UnMute : CommandModule<CommandContext>
    {
        [Command("unmute")]
        public static string unmute([CommandParameter(Remainder = true)] string playerId)
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
            Notification notification =
                new() { Id = 81, MessageEntry = "you have been unmuted, you can now chat again." };
            account.Home.NotificationFactory.Add(notification);

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage { Message = "You've been unmuted!" }
                );
                Sessions.Remove(lowID);
            }

            return $"Player with ID {playerId} has been unmuted.";
        }
    }

    public class UserInfo : CommandModule<CommandContext>
    {
        [Command("userinfo")]
        public static string userInfo([CommandParameter(Remainder = true)] string playerId)
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

            string ipAddress = ConvertInfoToData(account.Home.IpAddress);
            string lastLoginTime = account.Home.LastVisitHomeTime.ToString();
            string device = ConvertInfoToData(account.Home.Device);
            string name = ConvertInfoToData(account.Avatar.Name);
            string token = ConvertInfoToData(account.Avatar.PassToken);
            string soloWins = ConvertInfoToData(account.Avatar.SoloWins);
            string duoWins = ConvertInfoToData(account.Avatar.DuoWins);
            string trioWins = ConvertInfoToData(account.Avatar.TrioWins);
            string trophies = ConvertInfoToData(account.Avatar.Trophies);
            string banned = ConvertInfoToData(account.Avatar.Banned);
            string muted = ConvertInfoToData(account.Avatar.IsCommunityBanned);
            string username = DatabaseHelper.ExecuteScalar(
                "SELECT username FROM users WHERE id = @id",
                ("@id", lowID)
            );
            string password = DatabaseHelper.ExecuteScalar(
                "SELECT password FROM users WHERE id = @id",
                ("@id", lowID)
            );

            return $"# Information of {playerId}!\n"
                + $"IpAddress: {ipAddress}\n"
                + $"Last Login Time: {lastLoginTime} UTC\n"
                + $"Device: {device}\n"
                + $"# Account Stats\n"
                + $"Name: {name}\n"
                + $"Token: {token}\n"
                + $"Trophies: {trophies}\n"
                + $"Solo Wins: {soloWins}\n"
                + $"Duo Wins: {duoWins}\n"
                + $"Trio Wins: {trioWins}\n"
                + $"Muted: {muted}\n"
                + $"Banned: {banned}\n"
                + $"# royale ID\n"
                + $"Username: {username}\n"
                + $"Password: {password}";
        }

        private static string ConvertInfoToData(object data)
        {
            return data?.ToString() ?? "N/A";
        }
    }

    public class ResetSeason : CommandModule<CommandContext>
    {
        [Command("resetseason")]
        public static string resetseason()
        {
            long lastAccId2 = Accounts.GetMaxAvatarId();
            for (int accid = 1; accid <= lastAccId2; accid++)
            {
                Account thisAcc = Accounts.LoadNoChache(accid);
                if (thisAcc == null)
                    continue;

                if (thisAcc.Avatar.Trophies >= 550)
                {
                    List<int> hhh = new();
                    List<int> ht = new();
                    List<int> htr = new();
                    List<int> sa = new();
                    int[] StarPointsTrophiesStart =
                    {
                        550,
                        600,
                        650,
                        700,
                        750,
                        800,
                        850,
                        900,
                        950,
                        1000,
                        1050,
                        1100,
                        1150,
                        1200,
                        1250,
                        1300,
                        1350,
                        1400
                    };
                    int[] StarPointsTrophiesEnd =
                    {
                        599,
                        649,
                        699,
                        749,
                        799,
                        849,
                        899,
                        949,
                        999,
                        1049,
                        1099,
                        1149,
                        1199,
                        1249,
                        1299,
                        1349,
                        1399,
                        1000000
                    };
                    int[] StarPointsSeasonRewardAmount =
                    {
                        70,
                        120,
                        160,
                        200,
                        220,
                        240,
                        260,
                        280,
                        300,
                        320,
                        340,
                        360,
                        380,
                        400,
                        420,
                        440,
                        460,
                        480
                    };
                    int[] StarPointsTrophiesInReset =
                    {
                        525,
                        550,
                        600,
                        650,
                        700,
                        725,
                        750,
                        775,
                        800,
                        825,
                        850,
                        875,
                        900,
                        925,
                        950,
                        975,
                        1000,
                        1025
                    };

                    foreach (Hero h in thisAcc.Avatar.Heroes)
                    {
                        if (h.Trophies >= StarPointsTrophiesStart[0])
                        {
                            hhh.Add(h.CharacterId);
                            ht.Add(h.Trophies);
                            int i = 0;
                            while (true)
                            {
                                if (
                                    h.Trophies >= StarPointsTrophiesStart[i]
                                    && h.Trophies <= StarPointsTrophiesEnd[i]
                                )
                                {
                                    if (StarPointsTrophiesStart[i] != 1400)
                                    {
                                        htr.Add(h.Trophies - StarPointsTrophiesInReset[i]);
                                        h.Trophies = StarPointsTrophiesInReset[i];
                                        sa.Add(StarPointsSeasonRewardAmount[i]);
                                    }
                                    else
                                    {
                                        int b = h.Trophies - 1440;
                                        b /= 2;
                                        htr.Add(h.Trophies - StarPointsTrophiesInReset[i] - b);
                                        h.Trophies = (StarPointsTrophiesInReset[i] + b);
                                        sa.Add(StarPointsSeasonRewardAmount[i] + b / 2);
                                    }
                                    break;
                                }
                                else
                                {
                                    i++;
                                }
                            }
                        }
                    }

                    if (hhh.Count > 0)
                    {
                        thisAcc.Home.NotificationFactory.Add(
                            new Notification
                            {
                                Id = 79,
                                HeroesIds = hhh,
                                HeroesTrophies = ht,
                                HeroesTrophiesReseted = htr,
                                StarpointsAwarded = sa,
                            }
                        );
                    }
                }

                Accounts.Save(thisAcc);
                Console.WriteLine(accid);
            }

            return "Season reset completed for all players.";
        }
    }

    public class UnlockAll : CommandModule<CommandContext>
    {
        [Command("unlockall")]
        public static string UnlockAllCommand([CommandParameter(Remainder = true)] string playerId)
        {
            if (!playerId.StartsWith("#"))
            {
                return "Invalid player ID. Make sure it starts with '#'.";
            }

            long id = LogicLongCodeGenerator.ToId(playerId);
            Account account = Accounts.Load(id);

            if (account == null)
            {
                return $"Could not find player with ID {playerId}.";
            }

            try
            {
                account.Avatar.AddDiamonds(99999);
                account.Avatar.StarPoints += 99999;
                account.Avatar.AddGold(99999);

                List<int> allBrawlers =
                    new()
                    {
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        9,
                        10,
                        11,
                        12,
                        13,
                        14,
                        15,
                        16,
                        17,
                        18,
                        19,
                        20,
                        21,
                        22,
                        23,
                        24,
                        25,
                        26,
                        27,
                        28,
                        29,
                        30,
                        31,
                        32,
                        34,
                        35,
                        36,
                        37,
                        38,
                        39,
                        40,
                        41
                    };

                foreach (int brawlerId in allBrawlers)
                {
                    if (brawlerId == 0)
                    {
                        CharacterData character = DataTables
                            .Get(16)
                            .GetDataWithId<CharacterData>(0);
                        if (character == null)
                            continue;

                        CardData card = DataTables
                            .Get(23)
                            .GetData<CardData>(character.Name + "_unlock");
                        if (card == null)
                            continue;

                        account.Avatar.UnlockHero(character.GetGlobalId(), card.GetGlobalId());

                        Hero hero = account.Avatar.GetHero(character.GetGlobalId());
                        if (hero != null)
                        {
                            hero.PowerPoints = 860;
                            hero.PowerLevel = 8;
                            hero.HasStarpower = true;

                            CardData starPower1 = DataTables
                                .Get(23)
                                .GetData<CardData>(character.Name + "_unique");
                            CardData starPower2 = DataTables
                                .Get(23)
                                .GetData<CardData>(character.Name + "_unique_2");
                            CardData starPower3 = DataTables
                                .Get(23)
                                .GetData<CardData>(character.Name + "_unique_3");

                            if (starPower1 != null)
                                account.Avatar.Starpowers.Add(starPower1.GetGlobalId());
                            if (starPower2 != null)
                                account.Avatar.Starpowers.Add(starPower2.GetGlobalId());
                            if (starPower3 != null && !starPower3.LockedForChronos)
                                account.Avatar.Starpowers.Add(starPower3.GetGlobalId());

                            string[] gadgets =
                            {
                                "GrowBush",
                                "Shield",
                                "Heal",
                                "Jump",
                                "ShootAround",
                                "DestroyPet",
                                "PetSlam",
                                "Slow",
                                "Push",
                                "Dash",
                                "SpeedBoost",
                                "BurstHeal",
                                "Spin",
                                "Teleport",
                                "Immunity",
                                "Trail",
                                "Totem",
                                "Grab",
                                "Swing",
                                "Vision",
                                "Regen",
                                "HandGun",
                                "Promote",
                                "Sleep",
                                "Slow",
                                "Reload",
                                "Fake",
                                "Trampoline",
                                "Explode",
                                "Blink",
                                "PoisonTrigger",
                                "Barrage",
                                "Focus",
                                "MineTrigger",
                                "Reload",
                                "Seeker",
                                "Meteor",
                                "HealPotion",
                                "Stun",
                                "TurretBuff",
                                "StaticDamage"
                            };
                            string characterName =
                                char.ToUpper(character.Name[0]) + character.Name.Substring(1);
                            foreach (string gadgetName in gadgets)
                            {
                                CardData gadget = DataTables
                                    .Get(23)
                                    .GetData<CardData>(characterName + "_" + gadgetName);
                                if (gadget != null)
                                    account.Avatar.Starpowers.Add(gadget.GetGlobalId());
                            }
                        }
                        continue;
                    }

                    if (!account.Avatar.HasHero(16000000 + brawlerId))
                    {
                        CharacterData character = DataTables
                            .Get(16)
                            .GetDataWithId<CharacterData>(brawlerId);
                        if (character == null)
                            continue;

                        CardData card = DataTables
                            .Get(23)
                            .GetData<CardData>(character.Name + "_unlock");
                        if (card == null)
                            continue;

                        account.Avatar.UnlockHero(character.GetGlobalId(), card.GetGlobalId());

                        Hero hero = account.Avatar.GetHero(character.GetGlobalId());
                        if (hero != null)
                        {
                            hero.PowerPoints = 860;
                            hero.PowerLevel = 8;
                            hero.HasStarpower = true;

                            CardData starPower1 = DataTables
                                .Get(23)
                                .GetData<CardData>(character.Name + "_unique");
                            CardData starPower2 = DataTables
                                .Get(23)
                                .GetData<CardData>(character.Name + "_unique_2");
                            CardData starPower3 = DataTables
                                .Get(23)
                                .GetData<CardData>(character.Name + "_unique_3");

                            if (starPower1 != null)
                                account.Avatar.Starpowers.Add(starPower1.GetGlobalId());
                            if (starPower2 != null)
                                account.Avatar.Starpowers.Add(starPower2.GetGlobalId());
                            if (starPower3 != null && !starPower3.LockedForChronos)
                                account.Avatar.Starpowers.Add(starPower3.GetGlobalId());

                            string[] gadgets =
                            {
                                "GrowBush",
                                "Shield",
                                "Heal",
                                "Jump",
                                "ShootAround",
                                "DestroyPet",
                                "PetSlam",
                                "Slow",
                                "Push",
                                "Dash",
                                "SpeedBoost",
                                "BurstHeal",
                                "Spin",
                                "Teleport",
                                "Immunity",
                                "Trail",
                                "Totem",
                                "Grab",
                                "Swing",
                                "Vision",
                                "Regen",
                                "HandGun",
                                "Promote",
                                "Sleep",
                                "Slow",
                                "Reload",
                                "Fake",
                                "Trampoline",
                                "Explode",
                                "Blink",
                                "PoisonTrigger",
                                "Barrage",
                                "Focus",
                                "MineTrigger",
                                "Reload",
                                "Seeker",
                                "Meteor",
                                "HealPotion",
                                "Stun",
                                "TurretBuff",
                                "StaticDamage"
                            };

                            string characterName =
                                char.ToUpper(character.Name[0]) + character.Name.Substring(1);
                            foreach (string gadgetName in gadgets)
                            {
                                CardData gadget = DataTables
                                    .Get(23)
                                    .GetData<CardData>(characterName + "_" + gadgetName);
                                if (gadget != null)
                                    account.Avatar.Starpowers.Add(gadget.GetGlobalId());
                            }
                        }
                    }
                }

                List<string> skins =
                    new()
                    {
                        "Witch",
                        "Rockstar",
                        "Beach",
                        "Pink",
                        "Panda",
                        "White",
                        "Hair",
                        "Gold",
                        "Rudo",
                        "Bandita",
                        "Rey",
                        "Knight",
                        "Caveman",
                        "Dragon",
                        "Summer",
                        "Summertime",
                        "Pheonix",
                        "Greaser",
                        "GirlPrereg",
                        "Box",
                        "Santa",
                        "Chef",
                        "Boombox",
                        "Wizard",
                        "Reindeer",
                        "GalElf",
                        "Hat",
                        "Footbull",
                        "Popcorn",
                        "Hanbok",
                        "Cny",
                        "Valentine",
                        "WarsBox",
                        "Nightwitch",
                        "Cart",
                        "Shiba",
                        "GalBunny",
                        "Ms",
                        "GirlHotrod",
                        "Maple",
                        "RR",
                        "Mecha",
                        "MechaWhite",
                        "MechaNight",
                        "FootbullBlue",
                        "Outlaw",
                        "Hogrider",
                        "BoosterDefault",
                        "Shark",
                        "HoleBlue",
                        "BoxMoonFestival",
                        "WizardRed",
                        "Pirate",
                        "GirlWitch",
                        "KnightDark",
                        "DragonDark",
                        "DJ",
                        "Wolf",
                        "Brown",
                        "Total",
                        "Sally",
                        "Leonard",
                        "SantaRope",
                        "Gift",
                        "GT",
                        "Virus",
                        "BoosterVirus",
                        "Gamer",
                        "Valentines",
                        "Koala",
                        "BearKoala",
                        "AgentP",
                        "Football",
                        "Arena",
                        "Tanuki",
                        "Horus",
                        "ArenaPSG",
                        "DarkBunny",
                        "College",
                        "Bazaar",
                        "RedDragon",
                        "Constructor",
                        "Hawaii",
                        "Barbking",
                        "Trader",
                        "StationSummer",
                        "Silver",
                        "Bank",
                        "Retro",
                        "Ranger",
                        "Tracksuit",
                        "Knight",
                        "RetroAddon"
                    };

                foreach (Hero hero in account.Avatar.Heroes)
                {
                    CharacterData c = DataTables
                        .Get(DataType.Character)
                        .GetDataByGlobalId<CharacterData>(hero.CharacterId);
                    string cn = c.Name;
                    foreach (string name in skins)
                    {
                        SkinData s = DataTables.Get(DataType.Skin).GetData<SkinData>(cn + name);
                        if (s != null && !account.Home.UnlockedSkins.Contains(s.GetGlobalId()))
                        {
                            account.Home.UnlockedSkins.Add(s.GetGlobalId());
                        }
                    }
                }
                if (Sessions.IsSessionActive(id))
                {
                    var session = Sessions.GetSession(id);
                    session.GameListener.SendTCPMessage(
                        new AuthenticationFailedMessage
                        {
                            Message =
                                "Your account has been updated with everything unlocked and maxed!"
                        }
                    );
                    Sessions.Remove(id);
                }

                return $"Successfully unlocked and maxed everything for player with ID {playerId}.";
            }
            catch (Exception ex)
            {
                return $"An error occurred while unlocking content: {ex.Message}";
            }
        }
    }

    public class Status : CommandModule<CommandContext>
    {
        [Command("status")]
        public static string status()
        {
            long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
            DateTime startTime = Process.GetCurrentProcess().StartTime;
            DateTime now = DateTime.Now;

            TimeSpan uptime = now - startTime;

            string formattedUptime = string.Format(
                "{0}{1}{2}{3}",
                uptime.Days > 0 ? $"{uptime.Days} Days, " : string.Empty,
                uptime.Hours > 0 || uptime.Days > 0 ? $"{uptime.Hours} Hours, " : string.Empty,
                uptime.Minutes > 0 || uptime.Hours > 0
                  ? $"{uptime.Minutes} Minutes, "
                  : string.Empty,
                uptime.Seconds > 0 ? $"{uptime.Seconds} Seconds" : string.Empty
            );

            return "# Server Status\n"
                + $"Server Game Version: v29.270\n"
                + $"Server Build: v1.0 from 10.02.2024\n"
                + $"Resources Sha: {Fingerprint.Sha}\n"
                + $"Environment: Prod\n"
                + $"Server Time: {now} UTC\n"
                + $"Players Online: {Sessions.Count}\n"
                + $"Memory Used: {megabytesUsed} MB\n"
                + $"Uptime: {formattedUptime}\n"
                + $"Accounts Cached: {AccountCache.Count}\n"
                + $"Alliances Cached: {AllianceCache.Count}\n"
                + $"Teams Cached: {Teams.Count}\n";
        }
    }

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

            Notification nGems = new Notification
            {
                Id = 89,
                DonationCount = donationAmount,
                MessageEntry = $"<c6>received {donationAmount} gems, enjoy!</c>"
            };
            account.Home.NotificationFactory.Add(nGems);
            LogicAddNotificationCommand acmGems = new() { Notification = nGems };
            AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
            asmGems.Command = acmGems;

            if (Sessions.IsSessionActive(lowID))
            {
                var sessionGems = Sessions.GetSession(lowID);
                sessionGems.GameListener.SendTCPMessage(asmGems);
            }

            return $"Granted {donationAmount} gems to player with ID {parts[0]}.";
        }
    }

    public class SetTrophies : CommandModule<CommandContext> //TODO add confirmation
    {
        [Command("settrophies")]
        public static string settrophies(
            [CommandParameter(Remainder = true)] string playerIdAndTrophyCount
        )
        {
            string[] parts = playerIdAndTrophyCount.Split(' ');
            if (
                parts.Length != 2
                || !parts[0].StartsWith("#")
                || !int.TryParse(parts[1], out int trophyCount)
            )
            {
                return "Usage: !settrophies [TAG] [amount]";
            }

            long lowID = LogicLongCodeGenerator.ToId(parts[0]);
            Account account = Accounts.Load(lowID);

            if (account == null)
            {
                return $"Could not find player with ID {parts[0]}.";
            }

            account.Avatar.SetTrophies(trophyCount);

            if (Sessions.IsSessionActive(lowID))
            {
                var session = Sessions.GetSession(lowID);
                session.GameListener.SendTCPMessage(
                    new AuthenticationFailedMessage()
                    {
                        Message =
                            $"Your account updated! You now have {trophyCount} trophies on every of your brawlers!"
                    }
                );
                Sessions.Remove(lowID);
            }

            return $"Set {trophyCount} trophies on every brawler for player with ID {parts[0]}.";
        }
    }

    public class GivePremium : CommandModule<CommandContext>
    {
        [Command("givepremium")]
        public static string GivePremiumCommand(
            [CommandParameter(Remainder = true)] string playerId
        )
        {
            string[] parts = playerId.Split(' ');
            if (parts.Length != 1)
            {
                return "Usage: !givepremium [TAG]";
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

            Account account = Accounts.Load(id);
            if (account == null)
            {
                return $"Could not find player with ID {parts[0]}.";
            }

            if (account.Home.PremiumEndTime < DateTime.UtcNow)
            {
                account.Home.PremiumEndTime = DateTime.UtcNow.AddMonths(1);
            }
            else
            {
                account.Home.PremiumEndTime = account.Home.PremiumEndTime.AddMonths(1);
            }

            account.Avatar.PremiumLevel = 1;

            string formattedDate = account.Home.PremiumEndTime.ToString("dd'th of' MMMM yyyy");

            Notification n = new Notification
            {
                Id = 89,
                DonationCount = 40,
                MessageEntry =
                    $"<c6>Vip status activated/extended to {account.Home.PremiumEndTime} UTC! ({formattedDate})</c>"
            };

            account.Home.NotificationFactory.Add(n);

            LogicAddNotificationCommand acm = new LogicAddNotificationCommand { Notification = n };

            AvailableServerCommandMessage asm = new AvailableServerCommandMessage { Command = acm };

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(asm);
            }

            string d = sc ? LogicLongCodeGenerator.ToCode(id) : parts[0];
            return $"Done: set vip status for {d} activated/extended to {account.Home.PremiumEndTime} UTC! ({formattedDate})";
        }
    }

    public class ChangeUserCredentials : CommandModule<CommandContext>
    {
        [Command("changecredentials")]
        public static string ChangeUserCredentialsCommand(
            [CommandParameter(Remainder = true)] string input
        )
        {
            string[] parts = input.Split(' ');
            if (parts.Length != 3)
            {
                return "Usage: !changecredentials [TAG] [newUsername] [newPassword]";
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

            Account account = Accounts.Load(id);
            if (account == null)
            {
                return $"Could not find player with ID {parts[0]}.";
            }

            string newUsername = parts[1];
            string newPassword = parts[2];

            bool success = DatabaseHelper.ExecuteNonQuery(
                "UPDATE users SET username = @username, password = @password WHERE id = @id",
                ("@username", newUsername),
                ("@password", newPassword),
                ("@id", id)
            );

            if (!success)
            {
                return $"Failed to update credentials for player with ID {parts[0]}.";
            }

            string d = sc ? LogicLongCodeGenerator.ToCode(id) : parts[0];
            return $"Credentials for {d} updated: Username = {newUsername}, Password = {newPassword}";
        }
    }
    public class Reports : CommandModule<CommandContext> //TODO don't use litterbox api and send directly through discord
    {
        [Command("reports")]
        public static async Task<string> reports()
        {
            string filePath = "reports.txt";

            if (!File.Exists(filePath))
            {
                return "The reports file does not exist / no reports have been made yet";
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                        ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                        content.Add(fileContent, "fileToUpload", Path.GetFileName(filePath));

                        content.Add(new StringContent("fileupload"), "reqtype");
                        content.Add(new StringContent("72h"), "time");

                        // litterbox api
                        HttpResponseMessage response = await client.PostAsync(
                            "https://litterbox.catbox.moe/resources/internals/api.php",
                            content
                        );

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            return $"Reports uploaded to: {responseBody}";
                        }
                        else
                        {
                            return $"Failed to upload reports file to Litterbox. Status code: {response.StatusCode}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred while uploading the reports file: {ex.Message}";
            }
        }
    }
}
