using Discord.Commands;
using System.Threading.Tasks;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.Modules
{
    /// <summary>
    /// Text (!) versions of the general commands. Shared content (help / info) comes
    /// from BotResponses so it stays in sync with the slash (/) GeneralInteractionModule.
    /// </summary>
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Check bot latency")]
        public async Task PingAsync()
            => await ReplyAsync($"🏓 Pong! Latency: {Context.Client.Latency}ms");

        [Command("hello")]
        [Alias("hi", "hey")]
        [Summary("Say hello to the bot")]
        public async Task HelloAsync()
            => await ReplyAsync($"👋 Hello {Context.User.Mention}! I'm ready to play music!");

        [Command("help")]
        [Summary("Display available commands")]
        public async Task HelpAsync()
            => await ReplyAsync(embed: BotResponses.HelpEmbed());

        [Command("info")]
        [Summary("Display bot information")]
        public async Task InfoAsync()
            => await ReplyAsync(embed: BotResponses.InfoEmbed(Context.Client));
    }
}
