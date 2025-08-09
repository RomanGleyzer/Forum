using Application.DTOs.Comments;
using Application.DTOs.Posts;
using Application.DTOs.Users;
using Application.Features.Posts.Commands;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public sealed class PostsProfile : Profile
{
    public PostsProfile()
    {
        CreateMap<CreatePostCommand, Post>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Author, opt => opt.Ignore())
            .ForMember(d => d.AuthorId, opt => opt.Ignore())
            .ForMember(d => d.CreationDate, opt => opt.Ignore());

        CreateMap<ApplicationUser, AuthorDto>();

        CreateMap<Comment, CommentDto>()
            .ForMember(d => d.Author, opt => opt.MapFrom(s => s.Author));

        CreateMap<Post, PostPageDto>()
            .ForMember(d => d.Author, opt => opt.MapFrom(s => s.Author))
            .ForMember(d => d.FeaturedComment,
                opt => opt.MapFrom(s => s.Comments
                    .OrderByDescending(c => c.CreationDate)
                    .FirstOrDefault()));
    }
}
