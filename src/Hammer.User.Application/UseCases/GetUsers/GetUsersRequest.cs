using System.ComponentModel.DataAnnotations;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.UseCases.GetUsers;

/// <summary>
///     Request DTO for paginated user list query.
/// </summary>
public sealed record GetUsersRequest(
    [Range(1, 10000)] int Page = 1,
    [Range(1, 100)] int Size = 20,
    UserStatus? Status = null);
