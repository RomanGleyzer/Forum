using Application.DTOs.Users;
using MediatR;

namespace Application.Features.Users.Queries;

public class GetCurrentUserProfileQuery : IRequest<ApplicationUserDto>;