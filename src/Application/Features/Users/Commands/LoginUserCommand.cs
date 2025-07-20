using MediatR;

namespace Application.Features.Users.Commands;

public record LoginUserCommand(string Login, string Password) : IRequest<string>;