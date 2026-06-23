using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.Modules
{
    /// <summary>
    /// Text (!) versions of the music commands. Thin wrappers over MusicService —
    /// the actual logic lives there and is shared with the slash (/) MusicInteractionModule.
    /// </summary>
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        private readonly MusicService _music;

        public MusicModule(MusicService music)
        {
            _music = music;
        }

        private Task ReplyWithAsync(CommandResponse response)
            => response.Embed != null
                ? ReplyAsync(embed: response.Embed)
                : ReplyAsync(response.Message ?? string.Empty);

        [Command("play")]
        [Alias("p")]
        [Summary("Play a song (SoundCloud search) or a direct URL")]
        public async Task PlayAsync([Remainder] string searchQuery)
            => await ReplyWithAsync(await _music.PlayAsync(Context.Guild, Context.User as IVoiceState, Context.Channel.Id, searchQuery));

        [Command("join")]
        [Alias("connect")]
        [Summary("Join your voice channel")]
        public async Task JoinAsync()
            => await ReplyWithAsync(await _music.JoinAsync(Context.Guild, Context.User as IVoiceState, Context.Channel.Id));

        [Command("leave")]
        [Alias("disconnect", "dc")]
        [Summary("Leave the voice channel")]
        public async Task LeaveAsync()
            => await ReplyWithAsync(await _music.LeaveAsync(Context.Guild, Context.User as IVoiceState));

        [Command("pause")]
        [Summary("Pause the current track")]
        public async Task PauseAsync()
            => await ReplyWithAsync(await _music.PauseAsync(Context.Guild));

        [Command("resume")]
        [Summary("Resume the current track")]
        public async Task ResumeAsync()
            => await ReplyWithAsync(await _music.ResumeAsync(Context.Guild));

        [Command("stop")]
        [Summary("Stop playback and clear queue")]
        public async Task StopAsync()
            => await ReplyWithAsync(await _music.StopAsync(Context.Guild));

        [Command("skip")]
        [Alias("next", "s")]
        [Summary("Skip to the next track")]
        public async Task SkipAsync()
            => await ReplyWithAsync(await _music.SkipAsync(Context.Guild));

        [Command("queue")]
        [Alias("q")]
        [Summary("Show the current queue")]
        public async Task QueueAsync()
            => await ReplyWithAsync(await _music.QueueAsync(Context.Guild));

        [Command("nowplaying")]
        [Alias("np", "current")]
        [Summary("Show currently playing track")]
        public async Task NowPlayingAsync()
            => await ReplyWithAsync(await _music.NowPlayingAsync(Context.Guild, Context.User.Username));

        [Command("volume")]
        [Alias("vol")]
        [Summary("Set volume (0-150)")]
        public async Task VolumeAsync(int volume)
            => await ReplyWithAsync(await _music.SetVolumeAsync(Context.Guild, volume));
    }
}
