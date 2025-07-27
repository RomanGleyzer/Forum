using Application.DTOs.Users;
using MediatR;

namespace Application.Features.Users.Commands;

public record UpdateUserCommand(string FirstName, string LastName, string Email, string About, DateOnly DateOfBirth) 
    : IRequest<ApplicationUserDto>;