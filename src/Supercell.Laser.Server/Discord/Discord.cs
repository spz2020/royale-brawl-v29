namespace Supercell.Laser.Server.DiscordBot
{
    using NetCord;
    using NetCord.Gateway;
    using NetCord.Services;
    using NetCord.Services.Commands;
    using Supercell.Laser.Server.Settings;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class DiscordBot
    {
        public async Task StartAsync()
        {
            GatewayClient client =
                new(
                    new BotToken(Configuration.Instance.BotToken),
                    new GatewayClientConfiguration()
                    {
                        Intents =
                            GatewayIntents.GuildMessages
                            | GatewayIntents.DirectMessages
                            | GatewayIntents.MessageContent,
                    }
                );

            CommandService<CommandContext> commandService = new();
            commandService.AddModules(typeof(DiscordBot).Assembly);

            client.MessageCreate += async message =>
            {
                // Use the channel ID from the configuration
                if (message.ChannelId != Configuration.Instance.ChannelId)
                    return;

                if (!message.Content.StartsWith('!') || message.Author.IsBot)
                    return;

                var result = await commandService.ExecuteAsync(
                    prefixLength: 1,
                    new CommandContext(message, client)
                );

                if (result is not IFailResult failResult)
                    return;

                try
                {
                    await message.ReplyAsync(failResult.Message);
                }
                catch { }
            };

            client.Log += message =>
            {
                string formattedMessage = $"[DISCORD] {message}";
                Console.WriteLine(formattedMessage);
                return default;
            };

            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}