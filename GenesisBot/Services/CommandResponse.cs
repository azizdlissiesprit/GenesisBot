using Discord;

namespace DiscordMusicBot.Services
{
    /// <summary>
    /// A reply produced by a command's shared logic, independent of whether it was
    /// triggered by a text (!) command or a slash (/) command. The module that
    /// invoked it decides how to actually send it.
    /// </summary>
    public sealed class CommandResponse
    {
        public string? Message { get; }
        public Embed? Embed { get; }

        private CommandResponse(string? message, Embed? embed)
        {
            Message = message;
            Embed = embed;
        }

        public static CommandResponse Text(string message) => new(message, null);
        public static CommandResponse Embedded(Embed embed) => new(null, embed);
    }
}
