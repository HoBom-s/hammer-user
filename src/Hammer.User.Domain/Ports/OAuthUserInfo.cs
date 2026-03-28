namespace Hammer.User.Domain.Ports;

/// <summary>
///     User information retrieved from an OAuth provider.
/// </summary>
public sealed record OAuthUserInfo(string ProviderSubjectId, string? Email, string? Nickname);
