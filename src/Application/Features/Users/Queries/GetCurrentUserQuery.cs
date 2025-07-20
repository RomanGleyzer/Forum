using Application.DTOs.Users;
using MediatR;

namespace Application.Features.Users.Queries;

public record GetCurrentUserQuery() : IRequest<CurrentUserDto>;