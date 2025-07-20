using Application.DTOs.Posts;
using Application.DTOs.Users;
using Application.Features.Posts.Commands;
using Application.Features.Users.Commands;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CreatePostCommand, Post>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreationDate, opt => opt.Ignore());

        CreateMap<Post, PostPageDto>()
            .ForMember(d => d.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(d => d.FeaturedComment,
                opt => opt.MapFrom(src => src.Comments
                    .OrderByDescending(c => c.CreationDate)
                    .FirstOrDefault()));

        CreateMap<RegisterUserCommand, ApplicationUser>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(src => src.Email));

        CreateMap<ApplicationUser, ApplicationUserDto>();
    }
}
