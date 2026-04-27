namespace Hammer.User.Infrastructure.OAuth;

/// <summary>
///     OAuth provider configuration settings.
/// </summary>
internal sealed class OAuthSettings
{
    /// <summary>
    ///     Gets the Google OAuth settings.
    /// </summary>
    public ProviderSettings Google { get; init; } = new();

    /// <summary>
    ///     Gets the Apple OAuth settings.
    /// </summary>
    public ProviderSettings Apple { get; init; } = new();

    /// <summary>
    ///     Per-provider settings.
    /// </summary>
    internal sealed class ProviderSettings
    {
        /// <summary>
        ///     Gets the OAuth client IDs (web + mobile).
        /// </summary>
        public string[] ClientIds { get; init; } = [];
    }
}
