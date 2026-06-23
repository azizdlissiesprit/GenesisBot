using Discord;
using Victoria;
using Victoria.Rest.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services
{
    /// <summary>
    /// Shared music command logic. Both the text (!) MusicModule and the slash (/)
    /// MusicInteractionModule call into this so the behaviour stays identical for both.
    /// Methods take only the data they need (guild / voice state / channel) so they
    /// don't depend on which command framework invoked them.
    /// </summary>
    public class MusicService
    {
        private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode;
        private readonly LavaLinkService _audioService;

        public MusicService(LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode, LavaLinkService audioService)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
        }

        public async Task<CommandResponse> JoinAsync(IGuild guild, IVoiceState? voiceState, ulong textChannelId)
        {
            if (voiceState?.VoiceChannel == null)
                return CommandResponse.Text("❌ You must be connected to a voice channel!");

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel);
                _audioService.TextChannels.TryAdd(guild.Id, textChannelId);
                return CommandResponse.Text($"✅ Joined **{voiceState.VoiceChannel.Name}**!");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        public async Task<CommandResponse> LeaveAsync(IGuild guild, IVoiceState? voiceState)
        {
            var voiceChannel = voiceState?.VoiceChannel;
            if (voiceChannel == null)
                return CommandResponse.Text("❌ You must be in a voice channel!");

            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
                _audioService.TextChannels.TryRemove(guild.Id, out _);
                return CommandResponse.Text($"✅ Left **{voiceChannel.Name}**!");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        public async Task<CommandResponse> PlayAsync(IGuild guild, IVoiceState? voiceState, ulong textChannelId, string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return CommandResponse.Text("❌ Please provide a search term or URL!");

            if (voiceState?.VoiceChannel == null)
                return CommandResponse.Text("❌ You must be connected to a voice channel!");

            // Get or create player
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null)
            {
                try
                {
                    player = await _lavaNode.JoinAsync(voiceState.VoiceChannel);
                    _audioService.TextChannels.TryAdd(guild.Id, textChannelId);
                }
                catch (Exception ex)
                {
                    return CommandResponse.Text($"❌ Failed to join voice channel: {ex.Message}");
                }
            }

            // Decide how to resolve the track:
            //   - plain text           -> SoundCloud search
            //   - SoundCloud/other URL -> load directly
            //   - YouTube URL          -> can't stream (broken cipher), so read its title from
            //                             YouTube's metadata and play the SoundCloud match instead
            var isUrl = searchQuery.StartsWith("http://") || searchQuery.StartsWith("https://");
            var isYouTubeUrl = isUrl && (searchQuery.Contains("youtube.com") || searchQuery.Contains("youtu.be"));

            SearchResponse searchResponse;
            if (isYouTubeUrl)
            {
                // Metadata (title) still resolves from YouTube even though playback is blocked.
                var ytInfo = await _lavaNode.LoadTrackAsync(searchQuery);
                var ytTrack = ytInfo.Tracks?.FirstOrDefault();
                if (ytTrack == null)
                    return CommandResponse.Text("❌ Couldn't read that YouTube link. Try the song name instead.");

                searchResponse = await _lavaNode.LoadTrackAsync($"scsearch:{ytTrack.Title}");
            }
            else if (isUrl)
            {
                searchResponse = await _lavaNode.LoadTrackAsync(searchQuery);
            }
            else
            {
                searchResponse = await _lavaNode.LoadTrackAsync($"scsearch:{searchQuery}");
            }

            if (searchResponse.Type is SearchType.Empty or SearchType.Error)
                return CommandResponse.Text($"❌ I couldn't find anything for `{searchQuery}`");

            var tracks = searchResponse.Tracks?.ToList() ?? new List<LavaTrack>();
            if (tracks.Count == 0)
                return CommandResponse.Text($"❌ No tracks found for `{searchQuery}`");

            // A playlist or album link (e.g. a Spotify playlist) loads many tracks — queue them all.
            if (searchResponse.Type == SearchType.Playlist && tracks.Count > 1)
            {
                var queue = player.GetQueue();
                var startIndex = 0;

                // Start the first track immediately if nothing is playing.
                if (player.Track == null)
                {
                    await player.PlayAsync(_lavaNode, tracks[0], noReplace: false);
                    startIndex = 1;
                }

                for (var i = startIndex; i < tracks.Count; i++)
                    queue.Enqueue(tracks[i]);

                var name = searchResponse.Playlist.Name;
                var source = string.IsNullOrWhiteSpace(name) ? "the playlist" : $"**{name}**";
                return CommandResponse.Text($"✅ Queued **{tracks.Count}** tracks from {source}.");
            }

            // Single track or search result → pick the best one. SoundCloud "official artist"
            // uploads are often Go+ preview-only (they cut off after ~30s), so prefer a full
            // user re-upload when one is available.
            var track = PickPlayableTrack(tracks);

            // If nothing is playing, play immediately; otherwise queue it.
            if (player.Track == null)
            {
                await player.PlayAsync(_lavaNode, track);

                var embed = new EmbedBuilder()
                    .WithTitle("🎵 Now Playing")
                    .WithDescription($"[{track.Title}]({track.Url})")
                    .AddField("Author", track.Author, inline: true)
                    .AddField("Duration", track.Duration.ToString(@"mm\:ss"), inline: true)
                    .WithColor(Color.Blue)
                    .WithThumbnailUrl(track.Artwork ?? "")
                    .WithCurrentTimestamp()
                    .Build();

                return CommandResponse.Embedded(embed);
            }

            player.GetQueue().Enqueue(track);
            var queuePosition = player.GetQueue().Count;
            return CommandResponse.Text($"✅ Added **{track.Title}** to queue (Position: {queuePosition})");
        }

        public async Task<CommandResponse> PauseAsync(IGuild guild)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null || player.Track == null)
                return CommandResponse.Text("❌ Nothing is playing!");

            if (player.IsPaused)
                return CommandResponse.Text("❌ Playback is already paused!");

            try
            {
                await player.PauseAsync(_lavaNode);
                return CommandResponse.Text($"⏸️ Paused **{player.Track.Title}**");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        public async Task<CommandResponse> ResumeAsync(IGuild guild)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null || player.Track == null)
                return CommandResponse.Text("❌ Nothing is playing!");

            if (!player.IsPaused)
                return CommandResponse.Text("❌ Playback is not paused!");

            try
            {
                await player.ResumeAsync(_lavaNode, player.Track);
                return CommandResponse.Text($"▶️ Resumed **{player.Track.Title}**");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        public async Task<CommandResponse> StopAsync(IGuild guild)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null)
                return CommandResponse.Text("❌ I'm not connected to a voice channel!");

            if (player.Track == null)
                return CommandResponse.Text("❌ Nothing is playing!");

            try
            {
                player.GetQueue().Clear();
                await player.StopAsync(_lavaNode, player.Track);
                return CommandResponse.Text("⏹️ Stopped playback and cleared queue.");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        public async Task<CommandResponse> SkipAsync(IGuild guild)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null || player.Track == null)
                return CommandResponse.Text("❌ Nothing is playing!");

            var queue = player.GetQueue();
            if (queue.Count == 0)
                return CommandResponse.Text("❌ No more tracks in queue!");

            try
            {
                var skipped = player.Track;

                if (!queue.TryDequeue(out var nextTrack))
                    return CommandResponse.Text("❌ No more tracks in queue!");

                // noReplace: false forces Lavalink to replace the currently playing track.
                // Victoria's built-in SkipAsync plays with noReplace: true, which Lavalink
                // ignores while a track is active ("Skipping play request because of noReplace"),
                // so the skip never actually changes the audio.
                await player.PlayAsync(_lavaNode, nextTrack, noReplace: false);

                return CommandResponse.Text($"⏭️ Skipped **{skipped.Title}**\n🎵 Now playing **{nextTrack.Title}**");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        public async Task<CommandResponse> QueueAsync(IGuild guild)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null)
                return CommandResponse.Text("❌ I'm not connected to a voice channel!");

            if (player.Track == null)
                return CommandResponse.Text("❌ Nothing is playing!");

            var queueList = player.GetQueue().ToList();

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Music Queue")
                .WithColor(Color.Purple);

            embed.AddField("Now Playing",
                $"[{player.Track.Title}]({player.Track.Url})\n" +
                $"Duration: {player.Track.Duration:mm\\:ss}");

            if (queueList.Count > 0)
            {
                var queueText = new StringBuilder();
                var displayCount = Math.Min(10, queueList.Count); // Show max 10 tracks

                for (int i = 0; i < displayCount; i++)
                {
                    var track = queueList[i];
                    queueText.AppendLine($"{i + 1}. [{track.Title}]({track.Url}) - {track.Duration:mm\\:ss}");
                }

                if (queueList.Count > 10)
                    queueText.AppendLine($"\n*...and {queueList.Count - 10} more tracks*");

                embed.AddField($"Up Next ({queueList.Count} tracks)", queueText.ToString());
            }
            else
            {
                embed.AddField("Up Next", "Queue is empty");
            }

            return CommandResponse.Embedded(embed.Build());
        }

        public async Task<CommandResponse> NowPlayingAsync(IGuild guild, string requestedBy)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null || player.Track == null)
                return CommandResponse.Text("❌ Nothing is playing!");

            var track = player.Track;

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Now Playing")
                .WithDescription($"[{track.Title}]({track.Url})")
                .AddField("Author", track.Author, inline: true)
                .AddField("Duration", track.Duration.ToString(@"mm\:ss"), inline: true)
                .WithColor(Color.Blue)
                .WithThumbnailUrl(track.Artwork ?? "")
                .WithFooter($"Requested by {requestedBy}")
                .WithCurrentTimestamp()
                .Build();

            return CommandResponse.Embedded(embed);
        }

        public async Task<CommandResponse> SetVolumeAsync(IGuild guild, int volume)
        {
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null)
                return CommandResponse.Text("❌ I'm not connected to a voice channel!");

            if (volume < 0 || volume > 150)
                return CommandResponse.Text("❌ Volume must be between 0 and 150!");

            try
            {
                await player.SetVolumeAsync(_lavaNode, volume);
                return CommandResponse.Text($"🔊 Volume set to **{volume}%**");
            }
            catch (Exception ex)
            {
                return CommandResponse.Text($"❌ Error: {ex.Message}");
            }
        }

        // SoundCloud "official artist" uploads are frequently Go+ / preview-only — they stream
        // ~30 seconds then stop. User re-uploads usually play in full, so prefer the first
        // non-"official" result and only fall back to the first if every result looks official.
        private static LavaTrack PickPlayableTrack(List<LavaTrack> tracks)
        {
            foreach (var track in tracks)
            {
                var url = track.Url ?? string.Empty;
                if (url.IndexOf("-official", StringComparison.OrdinalIgnoreCase) < 0 &&
                    url.IndexOf("/official", StringComparison.OrdinalIgnoreCase) < 0)
                    return track;
            }

            return tracks[0];
        }
    }
}
