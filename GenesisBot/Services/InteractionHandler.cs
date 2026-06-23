using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordMusicBot
{
    /// <summary>
    /// Wires up Discord slash (/) commands: discovers the InteractionModuleBase modules,
    /// routes incoming interactions to them, and registers the commands with Discord.
    /// </summary>
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        public InteractionHandler(DiscordSocketClient client, InteractionService interactions, IServiceProvider services)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            // Discover all InteractionModuleBase modules (the slash command modules)
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteractionAsync;
            _interactions.SlashCommandExecuted += OnSlashCommandExecutedAsync;
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                await _interactions.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Interaction failed: {ex.Message}");
            }
        }

        private Task OnSlashCommandExecutedAsync(SlashCommandInfo info, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                Console.WriteLine($"Slash command '{info?.Name}' failed: {result.ErrorReason}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers the slash commands to every guild the bot is in. Guild registration
        /// is instant (global registration can take up to an hour to appear). Call this
        /// once the client is Ready.
        /// </summary>
        public async Task RegisterCommandsAsync()
        {
            try
            {
                foreach (var guild in _client.Guilds)
                    await _interactions.RegisterCommandsToGuildAsync(guild.Id, deleteMissing: true);

                Console.WriteLine($"Slash commands registered to {_client.Guilds.Count} guild(s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to register slash commands: {ex.Message}");
                Console.WriteLine("   If this is a 'Missing Access' (403) error, the bot was invited without the");
                Console.WriteLine("   'applications.commands' scope. Re-invite it with that scope and restart.");
            }
        }
    }
}
