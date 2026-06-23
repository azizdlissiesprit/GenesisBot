using Discord;
using Discord.WebSocket;

namespace DiscordMusicBot.Services
{
    /// <summary>
    /// Builders for the general (non-music) replies, shared by the text (!) and
    /// slash (/) modules so both stay in sync.
    /// </summary>
    public static class BotResponses
    {
        public static Embed HelpEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("🎵 Genesis Bot Commands")
                .WithDescription("Every command works with both the `!` prefix and `/` slash commands. Aliases in parentheses:")
                .WithColor(Color.Blue)
                .AddField("🎶 Music",
                    "`play <song/URL>` *(p)* — Play a track or add it to the queue\n" +
                    "`join` *(connect)* — Join your voice channel\n" +
                    "`leave` *(disconnect, dc)* — Leave the voice channel\n" +
                    "`pause` — Pause the current track\n" +
                    "`resume` — Resume playback\n" +
                    "`skip` *(next, s)* — Skip to the next track\n" +
                    "`stop` — Stop playback and clear the queue\n" +
                    "`queue` *(q)* — Show the current queue\n" +
                    "`nowplaying` *(np, current)* — Show the current track\n" +
                    "`volume <0-150>` *(vol)* — Set the volume")
                .AddField("⚙️ General",
                    "`ping` — Check bot latency\n" +
                    "`hello` *(hi, hey)* — Say hello to the bot\n" +
                    "`info` — Display bot information\n" +
                    "`help` — Display this message")
                .WithFooter("Tip: join a voice channel, then !play <song> or /play")
                .WithCurrentTimestamp()
                .Build();
        }

        public static Embed InfoEmbed(DiscordSocketClient client)
        {
            return new EmbedBuilder()
                .WithTitle("ℹ️ Bot Information")
                .WithColor(Color.Green)
                .AddField("Bot Name", client.CurrentUser.Username, inline: true)
                .AddField("Servers", client.Guilds.Count, inline: true)
                .AddField("Created By", "Your Name", inline: true)
                .AddField("Framework", ".NET 8 + Discord.Net 3.19", inline: true)
                .WithThumbnailUrl(client.CurrentUser.GetAvatarUrl())
                .WithCurrentTimestamp()
                .Build();
        }
    }
}
