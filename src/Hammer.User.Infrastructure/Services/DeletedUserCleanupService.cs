using Hammer.User.Application.Common;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hammer.User.Infrastructure.Services;

/// <summary>
///     Background service that hard-deletes soft-deleted users after 7 days.
///     Runs once at startup, then daily at midnight KST.
/// </summary>
internal sealed partial class DeletedUserCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeletedUserCleanupService> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7);

    internal static TimeSpan CalculateDelayUntilNextMidnightKst()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var nowKst = TimeZoneInfo.ConvertTime(nowUtc, TimeZones.Korea);
        var nextMidnightKst = nowKst.Date.AddDays(1);
        var nextMidnightUtc = new DateTimeOffset(nextMidnightKst, TimeZones.Korea.GetUtcOffset(nextMidnightKst));
        return nextMidnightUtc - nowUtc;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupDeletedUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogCleanupError(ex);
            }

            var delay = CalculateDelayUntilNextMidnightKst();
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task CleanupDeletedUsersAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow - _retentionPeriod;
        var totalDeleted = 0;
        int deleted;

        do
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var users = await repository.GetDeletedUsersBeforeAsync(cutoff, BatchSize, cancellationToken);

            foreach (var user in users)
                await repository.HardDeleteAsync(user, cancellationToken);

            deleted = users.Count;
            totalDeleted += deleted;
        }
        while (deleted > 0);

        if (totalDeleted > 0)
            LogCleanupCompleted(totalDeleted);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "삭제된 유저 {Count}명 영구 삭제 완료")]
    private partial void LogCleanupCompleted(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "삭제된 유저 정리 중 오류 발생")]
    private partial void LogCleanupError(Exception exception);
}
