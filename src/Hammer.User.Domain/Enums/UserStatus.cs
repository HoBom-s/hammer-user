namespace Hammer.User.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>Account is active and usable.</summary>
    Active = 1,

    /// <summary>Account is temporarily suspended.</summary>
    Suspended = 2,

    /// <summary>Account is soft-deleted.</summary>
    Deleted = 3,
}
