using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record GetUserProfileQuery(int UserId) : IRequest<UserProfileDto>;
public sealed record UpdateUserNameCommand(int UserId, string Name) : IRequest<UserProfileDto>;
public sealed record UpdateUserEmailCommand(int UserId, string? Email) : IRequest<UserProfileDto>;
public sealed record ChangePasswordCommand(int UserId, string NewPassword) : IRequest<UserProfileDto>;
