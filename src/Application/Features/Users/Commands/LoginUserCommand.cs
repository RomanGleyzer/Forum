using MediatR;

namespace Application.Features.Users.Commands;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthTokenResponse>;