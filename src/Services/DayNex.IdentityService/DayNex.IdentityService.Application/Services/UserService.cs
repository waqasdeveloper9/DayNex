using DayNex.IdentityService.Application.DTOs;
using DayNex.IdentityService.Application.Interfaces;
using DayNex.IdentityService.Domain.Entities;
using DayNex.Shared.Contracts;
using DayNex.Shared.Contracts.Enums;
using Microsoft.Extensions.Logging;

namespace DayNex.IdentityService.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UserDto>> ProvisionFromExternalLoginAsync(
        ProvisionUserDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Users.GetByExternalIdAsync(dto.ExternalId, cancellationToken);
        if (existing is not null)
        {
            return Result<UserDto>.Success(MapToDto(existing));
        }

        var user = User.CreateSimpleUser(dto.ExternalId, dto.Email, dto.DisplayName);
        var freeSubscription = Subscription.CreateFree(user.Id);
        user.AttachSubscription(freeSubscription);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.Subscriptions.AddAsync(freeSubscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Provisioned new user {UserId} ({Email}) on Free tier", user.Id, user.Email);

        return Result<UserDto>.Success(MapToDto(user, freeSubscription));
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetWithSubscriptionAsync(userId, cancellationToken);
        return user is null
            ? Result<UserDto>.Failure($"User '{userId}' was not found.", "NOT_FOUND")
            : Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<UserDto>>.Success(users.Select(u => MapToDto(u)).ToList());
    }

    public async Task<Result<UserDto>> ChangeRoleAsync(
        Guid targetUserId, UserRole newRole, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var target = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null)
            return Result<UserDto>.Failure($"User '{targetUserId}' was not found.", "NOT_FOUND");

        try
        {
            target.ChangeRole(newRole);
        }
        catch (InvalidOperationException ex)
        {
            return Result<UserDto>.Failure(ex.Message, "INVALID_OPERATION");
        }

        _unitOfWork.Users.Update(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {RequestingUserId} changed role of {TargetUserId} to {NewRole}",
            requestingUserId, targetUserId, newRole);

        return Result<UserDto>.Success(MapToDto(target));
    }

    public async Task<Result> DeactivateAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var target = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null)
            return Result.Failure($"User '{targetUserId}' was not found.", "NOT_FOUND");

        target.Deactivate();
        _unitOfWork.Users.Update(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ActivateAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var target = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null)
            return Result.Failure($"User '{targetUserId}' was not found.", "NOT_FOUND");

        target.Activate();
        _unitOfWork.Users.Update(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static UserDto MapToDto(User user, Subscription? subscriptionOverride = null)
    {
        var subscription = subscriptionOverride ?? user.Subscription;
        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc,
            subscription is null ? null : MapSubscription(subscription));
    }

    private static SubscriptionDto MapSubscription(Subscription s) => new(
        s.Id, s.UserId, s.Tier, s.Status, s.StartDateUtc, s.EndDateUtc, s.IsEffectivelyPremium());
}
