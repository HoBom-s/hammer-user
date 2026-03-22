namespace Hammer.User.Domain.Enums;

/// <summary>
/// Supported OAuth identity providers.
/// </summary>
public enum OAuthProvider
{
    /// <summary>Google OAuth.</summary>
    Google = 1,

    /// <summary>Apple Sign-In.</summary>
    Apple = 2,

    /// <summary>Kakao OAuth.</summary>
    Kakao = 3,

    /// <summary>Naver OAuth.</summary>
    Naver = 4,
}
