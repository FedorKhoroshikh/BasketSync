using Application.Commands;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetUserProfileHandler(IUnitOfWork uow)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        var user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден");

        return new UserProfileDto(user.Id, user.Name, user.Email,
            HasPassword: user.PwdHash is not null,
            HasGoogle: user.GoogleId is not null);
    }
}

public class UpdateUserNameHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateUserNameCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateUserNameCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Name))
            throw new ArgumentException("Имя не может быть пустым");

        var user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден");

        user.SetName(cmd.Name.Trim());

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true
                                            || ex.InnerException?.Message.Contains("unique") == true
                                            || ex.InnerException?.Message.Contains("23505") == true)
        {
            throw new ConflictException($"Имя '{cmd.Name}' уже занято");
        }

        return new UserProfileDto(user.Id, user.Name, user.Email,
            HasPassword: user.PwdHash is not null,
            HasGoogle: user.GoogleId is not null);
    }
}

public class UpdateUserEmailHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateUserEmailCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateUserEmailCommand cmd, CancellationToken ct)
    {
        var user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден");

        user.SetEmail(cmd.Email?.Trim());

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true
                                            || ex.InnerException?.Message.Contains("unique") == true
                                            || ex.InnerException?.Message.Contains("23505") == true)
        {
            throw new ConflictException($"Email '{cmd.Email}' уже используется");
        }

        return new UserProfileDto(user.Id, user.Name, user.Email,
            HasPassword: user.PwdHash is not null,
            HasGoogle: user.GoogleId is not null);
    }
}

public class ChangePasswordHandler(IUnitOfWork uow, IPasswordHasher hasher)
    : IRequestHandler<ChangePasswordCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NewPassword) || cmd.NewPassword.Length < 4)
            throw new ArgumentException("Пароль должен содержать минимум 4 символа");

        var user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден");

        user.SetPwdHash(hasher.Hash(cmd.NewPassword));
        await uow.SaveChangesAsync(ct);

        return new UserProfileDto(user.Id, user.Name, user.Email,
            HasPassword: user.PwdHash is not null,
            HasGoogle: user.GoogleId is not null);
    }
}
