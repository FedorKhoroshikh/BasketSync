using Application.Commands;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class RegisterHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtService jwt)
    : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Name))
            throw new ArgumentException("Имя пользователя не может быть пустым");

        var hash = hasher.Hash(cmd.Password);
        var user = new User(cmd.Name.Trim(), hash);
        uow.Users.Add(user);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true
                                            || ex.InnerException?.Message.Contains("unique") == true
                                            || ex.InnerException?.Message.Contains("23505") == true)
        {
            throw new ConflictException($"Пользователь '{cmd.Name}' уже существует");
        }

        var token = jwt.GenerateToken(user.Id, user.Name);
        return new AuthResultDto(user.Id, user.Name, token);
    }
}

public class LoginHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtService jwt)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.Name == cmd.Name || u.Email == cmd.Name, ct)
            ?? throw new KeyNotFoundException($"Пользователь '{cmd.Name}' не найден");

        if (user.PwdHash is null)
            throw new UnauthorizedAccessException("Этот аккаунт использует вход через Google");

        if (!hasher.Verify(cmd.Password, user.PwdHash))
            throw new UnauthorizedAccessException("Неверный пароль");

        var token = jwt.GenerateToken(user.Id, user.Name);
        return new AuthResultDto(user.Id, user.Name, token);
    }
}

public class GoogleLoginHandler(IUnitOfWork uow, IGoogleTokenValidator google, IJwtService jwt)
    : IRequestHandler<GoogleLoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(GoogleLoginCommand cmd, CancellationToken ct)
    {
        var info = await google.ValidateAsync(cmd.IdToken, ct);

        // 1. Find by GoogleId → already linked
        var user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.GoogleId == info.Subject, ct);

        if (user is not null)
        {
            var token = jwt.GenerateToken(user.Id, user.Name);
            return new AuthResultDto(user.Id, user.Name, token);
        }

        // 2. Find by Email → link Google to existing account
        user = await uow.Users.GetAll()
            .FirstOrDefaultAsync(u => u.Email == info.Email, ct);

        if (user is not null)
        {
            user.LinkGoogle(info.Email, info.Subject);
            await uow.SaveChangesAsync(ct);
            var token = jwt.GenerateToken(user.Id, user.Name);
            return new AuthResultDto(user.Id, user.Name, token);
        }

        // 3. Create new user: Name = Email
        user = new User(info.Email, info.Email, info.Subject);
        uow.Users.Add(user);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true
                                            || ex.InnerException?.Message.Contains("unique") == true
                                            || ex.InnerException?.Message.Contains("23505") == true)
        {
            throw new ConflictException($"Пользователь с таким Google-аккаунтом уже существует");
        }

        var t = jwt.GenerateToken(user.Id, user.Name);
        return new AuthResultDto(user.Id, user.Name, t);
    }
}
