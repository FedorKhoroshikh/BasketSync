using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record RegisterCommand(string Name, string Password) : IRequest<AuthResultDto>;
public sealed record LoginCommand(string Name, string Password) : IRequest<AuthResultDto>;
public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthResultDto>;
