using Hammer.User.Domain.Common;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Entities;

/// <summary>
///     User aggregate root managing authentication, devices, and account lifecycle.
/// </summary>
public sealed class User : Entity
{
    private readonly List<OAuthAccount> _oAuthAccounts = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    /// <summary>
    ///     Gets the user's email address. Null for OAuth-only users without email (e.g. Kakao).
    /// </summary>
    public string? Email { get; private init; }

    /// <summary>
    ///     Gets the user's display nickname.
    /// </summary>
    public string Nickname { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the hashed password. Null for OAuth-only users.
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    ///     Gets the account status.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    ///     Gets the soft-deletion timestamp, or <c>null</c> if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    ///     Gets the terms of service version the user agreed to at registration.
    /// </summary>
    public string? AgreedTermsVersion { get; private set; }

    /// <summary>
    ///     Gets the user's registered device, or <c>null</c> if none. Single-device policy.
    /// </summary>
    public UserDevice? Device { get; private set; }

    /// <summary>
    ///     Gets the linked OAuth provider accounts.
    /// </summary>
    public IReadOnlyCollection<OAuthAccount> OAuthAccounts => _oAuthAccounts.AsReadOnly();

    /// <summary>
    ///     Gets the issued refresh tokens.
    /// </summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>
    ///     Gets a value indicating whether the account is soft-deleted.
    /// </summary>
    public bool IsDeleted => Status == UserStatus.Deleted;

    /// <summary>
    ///     Gets a value indicating whether the account is activating.
    /// </summary>
    public bool IsActive => Status == UserStatus.Active;

    /// <summary>
    ///     Creates a user with email/password credentials.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="nickname">The user's display nickname.</param>
    /// <param name="passwordHash">The pre-hashed password.</param>
    /// <param name="agreedTermsVersion">The terms of service version agreed to.</param>
    /// <returns>A new <see cref="User" /> instance.</returns>
    public static User CreateWithCredentials(string email, string nickname, string passwordHash, string? agreedTermsVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email = email,
            Nickname = nickname,
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            AgreedTermsVersion = agreedTermsVersion,
        };
    }

    /// <summary>
    ///     Creates a user via OAuth provider.
    /// </summary>
    /// <param name="nickname">The user's display nickname.</param>
    /// <param name="email">The user's email address, or <c>null</c> if not provided by the provider.</param>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="providerSubjectId">The provider's subject identifier.</param>
    /// <param name="agreedTermsVersion">The terms of service version agreed to.</param>
    /// <returns>A new <see cref="User" /> instance.</returns>
    public static User CreateWithOAuth(string nickname, string? email, OAuthProvider provider, string providerSubjectId, string? agreedTermsVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubjectId);

        User user = new() { Email = email, Nickname = nickname, Status = UserStatus.Active, AgreedTermsVersion = agreedTermsVersion };

        user._oAuthAccounts.Add(OAuthAccount.Create(user.Id, provider, providerSubjectId));
        return user;
    }

    /// <summary>
    ///     Links an OAuth provider account to this user.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="providerSubjectId">The provider's subject identifier.</param>
    public void LinkOAuthAccount(OAuthProvider provider, string providerSubjectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubjectId);

        if (HasOAuthProvider(provider))
            throw new InvalidOperationException($"OAuth provider {provider} is already linked.");

        _oAuthAccounts.Add(OAuthAccount.Create(Id, provider, providerSubjectId));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Checks whether this user has the specified OAuth provider linked.
    /// </summary>
    /// <param name="provider">The OAuth provider to check.</param>
    /// <returns><c>true</c> if the provider is linked; otherwise <c>false</c>.</returns>
    public bool HasOAuthProvider(OAuthProvider provider) =>
        _oAuthAccounts.Exists(a => a.Provider == provider);

    /// <summary>
    ///     Registers a device, replacing any existing device (single-device policy).
    /// </summary>
    /// <param name="platform">The device platform.</param>
    /// <param name="deviceIdentifier">The unique device identifier.</param>
    /// <param name="pushToken">The push token.</param>
    public void RegisterDevice(DevicePlatform platform, string deviceIdentifier, string pushToken)
    {
        Device = UserDevice.Create(Id, platform, deviceIdentifier, pushToken);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Updates the push token on the currently registered device.
    /// </summary>
    /// <param name="pushToken">The new push token.</param>
    public void UpdateDevicePushToken(string pushToken)
    {
        if (Device is null)
            throw new InvalidOperationException("No device is registered.");

        Device.UpdatePushToken(pushToken);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Removes the currently registered device.
    /// </summary>
    public void RemoveDevice()
    {
        Device = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Issues a new refresh token, revoking all existing tokens.
    /// </summary>
    /// <param name="token">The token value.</param>
    /// <param name="expiresAt">The expiration timestamp.</param>
    public void IssueRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RevokeAllRefreshTokens();
        _refreshTokens.Add(RefreshToken.Create(Id, token, expiresAt));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Updates the user's display nickname.
    /// </summary>
    /// <param name="nickname">The new nickname.</param>
    public void UpdateNickname(string nickname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        Nickname = nickname;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Sets the user's password hash.
    /// </summary>
    /// <param name="passwordHash">The new hashed password.</param>
    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Checks whether this user has a password set.
    /// </summary>
    /// <returns><c>true</c> if the user has a password; otherwise <c>false</c>.</returns>
    public bool HasPassword() => PasswordHash is not null;

    /// <summary>
    ///     Soft-deletes the account: sets status to deleted, revokes all tokens, and removes the device.
    /// </summary>
    public void SoftDelete()
    {
        Status = UserStatus.Deleted;
        DeletedAt = DateTimeOffset.UtcNow;
        RevokeAllRefreshTokens();
        RemoveDevice();
    }

    /// <summary>
    ///     Revokes all active refresh tokens.
    /// </summary>
    private void RevokeAllRefreshTokens()
    {
        foreach (var refreshToken in _refreshTokens)
            refreshToken.Revoke();
    }
}
