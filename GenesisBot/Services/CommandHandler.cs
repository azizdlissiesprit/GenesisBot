using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordMusicBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly BotConfig _config;

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services, BotConfig config)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _config = config;
        }

        public async Task InitializeAsync()
        {
            // Register command modules
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Subscribe to message received event
            _client.MessageReceived += HandleCommandAsync;

            // Subscribe to command executed event for logging
            _commands.CommandExecuted += OnCommandExecutedAsync;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (messageParam is not SocketUserMessage message) return;

            // Don't process if message is from a bot
            if (message.Author.IsBot) return;

            int argPos = 0;

            // Check if message starts with prefix or mentions the bot
            if (!message.HasStringPrefix(_config.Prefix, ref argPos) &&
                !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            // Create command context
            var context = new SocketCommandContext(_client, message);

            // Execute command
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // Log if command failed
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync($"❌ Error: {result.ErrorReason}");
                Console.WriteLine($"Command failed: {result.ErrorReason}");
            }
        }
    }
}
