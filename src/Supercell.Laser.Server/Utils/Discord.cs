using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Supercell.Laser.Server.DiscordBot
{
    public class DiscordBot
    {
        private string _botToken;
        private ulong _channelId;

        public static Task Main(string[] args) => new DiscordBot().MainAsync();

        public async Task MainAsync()
        {
            LoadConfig();

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (_, services) =>
                        services.AddSingleton(
                            x =>
                                new DiscordSocketClient(
                                    new DiscordSocketConfig
                                    {
                                        GatewayIntents = GatewayIntents.AllUnprivileged,
                                        AlwaysDownloadUsers = true,
                                    }
                                )
                        )
                )
                .Build();

            await RunAsync(host);
        }

        private void LoadConfig()
        {
            try
            {
                var configText = File.ReadAllText("config.json");
                dynamic config = JsonConvert.DeserializeObject(configText);
                _botToken = config.BotToken;
                _channelId = config.ChannelId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load config: {ex.Message}");
                Environment.Exit(1);
            }
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var _client = provider.GetRequiredService<DiscordSocketClient>();

            _client.Log += async (LogMessage msg) =>
            {
                // ignored msgs
                string[] ignoredMessages = new[]
                {
                    "Unknown Channel",
                    "GuildScheduledEvents gateway intent",
                    "GuildInvites gateway intent",
                    "Connected",
                    "Discord.Net v",
                    "Error handling Dispatch"
                };

                bool shouldIgnore = false;
                foreach (var ignoredMessage in ignoredMessages)
                {
                    if (msg.Message != null && msg.Message.Contains(ignoredMessage))
                    {
                        shouldIgnore = true;
                        break;
                    }
                }

                if (msg.Message.Contains("Disconnected"))
                {
                    Environment.Exit(1);
                }

                if (!shouldIgnore)
                {
                    Console.WriteLine("[DISCORD] " + msg.Message);
                }
            };

            _client.Ready += async () =>
            {
                // try to get channel
                var channel = await _client.GetChannelAsync(_channelId) as IMessageChannel;

                if (channel != null)
                {
                    try
                    {
                        // send msg in the channel
                        await channel.SendMessageAsync("control bot started!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DISCORD] Error sending message: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine(
                        "[DISCORD] Failed to find the channel or access it. Please check if the channel exists and if the bot has permission."
                    );
                }
            };

            try
            {
                await _client.LoginAsync(TokenType.Bot, _botToken);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Environment.Exit(1);
            }

            await Task.Delay(-1); // keep the bot running
        }
    }
}