using MediatR;

namespace Application.Features.Users.Commands;

public record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string Password,
    string ConfirmedPassword) : IRequest<string>;