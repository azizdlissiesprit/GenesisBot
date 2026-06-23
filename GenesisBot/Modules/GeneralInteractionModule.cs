using Discord.Interactions;
using System.Threading.Tasks;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.Modules
{
    /// <summary>
    /// Slash (/) versions of the general commands. Shared content (help / info) comes
    /// from BotResponses so it stays in sync with the text (!) GeneralModule.
    /// </summary>
    public class GeneralInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ping", "Check bot latency")]
        public async Task PingAsync()
            => await RespondAsync($"🏓 Pong! Latency: {Context.Client.Latency}ms");

        [SlashCommand("hello", "Say hello to the bot")]
        public async Task HelloAsync()
            => await RespondAsync($"👋 Hello {Context.User.Mention}! I'm ready to play music!");

        [SlashCommand("help", "Display available commands")]
        public async Task HelpAsync()
            => await RespondAsync(embed: BotResponses.HelpEmbed());

        [SlashCommand("info", "Display bot information")]
        public async Task InfoAsync()
            => await RespondAsync(embed: BotResponses.InfoEmbed(Context.Client));
    }
}
