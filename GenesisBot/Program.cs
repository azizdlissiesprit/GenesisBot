using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victoria;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordMusicBot.Services;

namespace DiscordMusicBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private InteractionService _interactions;
        private IServiceProvider _services;
        private bool _commandsRegistered;

        public async Task RunBotAsync()
        {
            // Load configuration
            var configJson = await File.ReadAllTextAsync("Config/config.json");
            var config = JsonSerializer.Deserialize<BotConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Initialize client
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });

            // Initialize commands
            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });

            // Initialize slash command (/) service
            _interactions = new InteractionService(_client, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info
            });

            // Setup dependency injection with Victoria 7.x
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(_interactions)
                .AddSingleton(config)
                .AddLogging(x =>
                {
                    x.AddConsole();
                    x.SetMinimumLevel(LogLevel.Information);
                })
                .AddLavaNode(x =>
                {
                    x.Hostname = config.Lavalink.Hostname;
                    x.Port = config.Lavalink.Port;
                    x.Authorization = config.Lavalink.Password;
                    x.IsSecure = config.Lavalink.Secure;
                    x.SelfDeaf = true;

                    // Victoria's default WebSocket buffer (2048 bytes) is too small for some
                    // Lavalink messages, and its multi-frame reassembly is buggy — it throws
                    // "Destination array was not long enough" and drops the connection into a
                    // reconnect loop. A larger buffer keeps each message in a single frame.
                    x.SocketConfiguration = new Victoria.WebSocket.Internal.WebSocketConfiguration
                    {
                        BufferSize = 16384,
                        ReconnectAttempts = 10,
                        ReconnectDelay = 3000
                    };
                })
                .AddSingleton<CommandHandler>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<MusicService>()
                .AddSingleton<LavaLinkService>()
                .BuildServiceProvider();

            // Subscribe to events
            _client.Log += Log;
            _client.Ready += OnReady;

            // Initialize command handler (text) and interaction handler (slash)
            await _services.GetRequiredService<CommandHandler>().InitializeAsync();
            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

            // Wire up Lavalink track events (queue auto-advance when a song finishes)
            await _services.GetRequiredService<LavaLinkService>().InitializeAsync();

            // Login and start
            await _client.LoginAsync(TokenType.Bot, config.Token);
            await _client.StartAsync();

            // Keep the bot running
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task OnReady()
        {
            Console.WriteLine($"Bot is connected and ready! Logged in as {_client.CurrentUser.Username}");

            // Connect to Lavalink using v7 extension
            await _services.UseLavaNodeAsync();

            // Register slash (/) commands with Discord. Ready can fire more than once
            // (reconnects), so only register on the first Ready.
            if (!_commandsRegistered)
            {
                _commandsRegistered = true;
                await _services.GetRequiredService<InteractionHandler>().RegisterCommandsAsync();
            }
        }
    }

    // Config classes
    public class BotConfig
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public LavalinkConfig Lavalink { get; set; }
    }

    public class LavalinkConfig
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public bool Secure { get; set; }
    }
}
