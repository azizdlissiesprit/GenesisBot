using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.Modules
{
    /// <summary>
    /// Slash (/) versions of the music commands. Thin wrappers over MusicService —
    /// the actual logic lives there and is shared with the text (!) MusicModule.
    /// </summary>
    public class MusicInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly MusicService _music;

        public MusicInteractionModule(MusicService music)
        {
            _music = music;
        }

        // Music commands can take a moment (Lavalink search / voice join), so defer first
        // to avoid the 3-second interaction timeout, then follow up with the result.
        private async Task RespondWithAsync(CommandResponse response)
        {
            if (response.Embed != null)
                await FollowupAsync(embed: response.Embed);
            else
                await FollowupAsync(response.Message ?? string.Empty);
        }

        [SlashCommand("play", "Play a song or add it to the queue")]
        public async Task PlayAsync([Summary(description: "Song name or URL")] string query)
        {
            await DeferAsync();
            await RespondWithAsync(await _music.PlayAsync(Context.Guild, Context.User as IVoiceState, Context.Channel.Id, query));
        }

        [SlashCommand("join", "Join your voice channel")]
        public async Task JoinAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.JoinAsync(Context.Guild, Context.User as IVoiceState, Context.Channel.Id));
        }

        [SlashCommand("leave", "Leave the voice channel")]
        public async Task LeaveAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.LeaveAsync(Context.Guild, Context.User as IVoiceState));
        }

        [SlashCommand("pause", "Pause the current track")]
        public async Task PauseAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.PauseAsync(Context.Guild));
        }

        [SlashCommand("resume", "Resume playback")]
        public async Task ResumeAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.ResumeAsync(Context.Guild));
        }

        [SlashCommand("skip", "Skip to the next track")]
        public async Task SkipAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.SkipAsync(Context.Guild));
        }

        [SlashCommand("stop", "Stop playback and clear the queue")]
        public async Task StopAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.StopAsync(Context.Guild));
        }

        [SlashCommand("queue", "Show the current queue")]
        public async Task QueueAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.QueueAsync(Context.Guild));
        }

        [SlashCommand("nowplaying", "Show the currently playing track")]
        public async Task NowPlayingAsync()
        {
            await DeferAsync();
            await RespondWithAsync(await _music.NowPlayingAsync(Context.Guild, Context.User.Username));
        }

        [SlashCommand("volume", "Set the volume (0-150)")]
        public async Task VolumeAsync([Summary(description: "Volume from 0 to 150")] int volume)
        {
            await DeferAsync();
            await RespondWithAsync(await _music.SetVolumeAsync(Context.Guild, volume));
        }
    }
}
