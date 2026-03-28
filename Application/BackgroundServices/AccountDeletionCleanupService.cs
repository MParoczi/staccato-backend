using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.BackgroundServices;

public sealed class AccountDeletionCleanupService(
    IServiceScopeFactory scopeFactory,
    IAzureBlobService blobService,
    ILogger<AccountDeletionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<DomainModels.Models.User> expiredUsers;
        try
        {
            expiredUsers = await userRepo.GetExpiredForDeletionAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query expired users for deletion.");
            return;
        }

        if (expiredUsers.Count == 0)
            return;

        foreach (var user in expiredUsers)
        {
            if (user.AvatarUrl is not null)
            {
                try
                {
                    await blobService.DeleteAsync($"avatars/{user.Id}", ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete avatar blob for user {UserId}. Continuing.", user.Id);
                }
            }

            userRepo.Remove(user);
        }

        try
        {
            await uow.CommitAsync(ct);
            logger.LogInformation("Deleted {Count} expired user account(s).", expiredUsers.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to commit user deletion batch. Accounts will be retried on the next run.");
        }
    }
}
