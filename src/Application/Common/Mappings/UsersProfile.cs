using Application.DTOs.Users;
using Application.Features.Users.Commands;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public sealed class UsersProfile : Profile
{
    public UsersProfile()
    {
        CreateMap<RegisterUserCommand, ApplicationUser>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(src => src.Email));

        CreateMap<ApplicationUser, CurrentUserDto>();
        CreateMap<ApplicationUser, ApplicationUserDto>();
        CreateMap<UpdateUserCommand, ApplicationUser>();
    }
}