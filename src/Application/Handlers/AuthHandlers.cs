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
            .FirstOrDefaultAsync(u => u.Name == cmd.Name, ct)
            ?? throw new KeyNotFoundException($"Пользователь '{cmd.Name}' не найден");

        if (!hasher.Verify(cmd.Password, user.PwdHash))
            throw new UnauthorizedAccessException("Неверный пароль");

        var token = jwt.GenerateToken(user.Id, user.Name);
        return new AuthResultDto(user.Id, user.Name, token);
    }
}
