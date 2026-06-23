using Discord;
using Discord.WebSocket;
using Victoria;
using Victoria.WebSocket;
using Victoria.WebSocket.EventArgs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Victoria.Enums;

namespace DiscordMusicBot.Services
{
    public class LavaLinkService
    {
        private readonly DiscordSocketClient _client;
        private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode;
        private readonly ILogger<LavaLinkService> _logger;

        // Store text channel IDs for each guild so we can send messages
        public readonly ConcurrentDictionary<ulong, ulong> TextChannels;

        public LavaLinkService(
            DiscordSocketClient client,
            LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode,
            ILogger<LavaLinkService> logger)
        {
            _client = client;
            _lavaNode = lavaNode;
            _logger = logger;
            TextChannels = new ConcurrentDictionary<ulong, ulong>();
        }

        public Task InitializeAsync()
        {
            // Auto-advance the queue when a track finishes, and log voice socket closures.
            // OnTrackStart is intentionally NOT subscribed: the play command already posts the
            // "Now Playing" message, so subscribing here would double-post it on every play.
            _lavaNode.OnTrackEnd += OnTrackEndAsync;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;

            _logger.LogInformation("LavaLink service initialized with event handlers.");
            return Task.CompletedTask;
        }

        private async Task OnTrackStartAsync(TrackStartEventArg arg)
        {
            _logger.LogInformation($"Now playing: {arg.Track.Title} in guild {arg.GuildId}");

            // Send message to text channel if we know which one
            if (TextChannels.TryGetValue(arg.GuildId, out var channelId))
            {
                var guild = _client.GetGuild(arg.GuildId);
                var channel = guild?.GetTextChannel(channelId);

                if (channel != null)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("🎵 Now Playing")
                        .WithDescription($"[{arg.Track.Title}]({arg.Track.Url})")
                        .AddField("Author", arg.Track.Author, inline: true)
                        .AddField("Duration", arg.Track.Duration.ToString(@"mm\:ss"), inline: true)
                        .WithColor(Color.Blue)
                        .WithThumbnailUrl(arg.Track.Artwork ?? "") // Use Artwork instead of ArtworkUrl
                        .WithCurrentTimestamp();

                    await channel.SendMessageAsync(embed: embed.Build());
                }
            }
        }

        private async Task OnTrackEndAsync(TrackEndEventArg arg)
        {
            _logger.LogInformation($"Track ended: {arg.Track.Title} - Reason: {arg.Reason}");

            // If track ended normally and there's more in queue, play next
            if (arg.Reason == TrackEndReason.Finished) // Fixed namespace
            {
                var player = await _lavaNode.TryGetPlayerAsync(arg.GuildId);
                if (player != null)
                {
                    var queue = player.GetQueue();
                    if (queue.TryDequeue(out var nextTrack))
                    {
                        await player.PlayAsync(_lavaNode, nextTrack);
                        _logger.LogInformation($"Playing next track: {nextTrack.Title}");
                    }
                    else
                    {
                        _logger.LogInformation("Queue is empty, playback stopped.");
                    }
                }
            }
        }

        private Task OnWebSocketClosedAsync(WebSocketClosedEventArg arg)
        {
            _logger.LogWarning($"WebSocket closed - Code: {arg.Code}, Reason: {arg.Reason}");
            return Task.CompletedTask;
        }
    }
}
