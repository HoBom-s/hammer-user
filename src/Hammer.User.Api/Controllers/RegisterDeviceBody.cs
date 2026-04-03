using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Request body for device registration.
/// </summary>
public sealed record RegisterDeviceBody(
    [property: JsonRequired] DevicePlatform Platform,
    [Required] string DeviceIdentifier,
    [Required] string PushToken);
