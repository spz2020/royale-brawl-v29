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

    public class CustomLogger
    {
        public void Log(LogSeverity severity, string message, Exception? exception = null)
        {
            string formattedMessage = $"[DISCORD] {message}";

            if (severity == LogSeverity.Info)
            {
                Console.WriteLine(formattedMessage);
            }
            else
            {
                Console.WriteLine($"[{severity}] {formattedMessage}");
            }
        }
    }

    public class DiscordBot
    {
        private readonly CustomLogger _logger = new CustomLogger();

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
                _logger.Log(message.Severity, message.Message, message.Exception);
                return default;
            };

            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
