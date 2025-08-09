using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Exceptions;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Features.Posts.Commands;

public class CreatePostCommandHandler(
    ICurrentUserService currentUser,
    IPostRepository postRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreatePostCommandHandler> logger,
    IMapper mapper,
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<CreatePostCommand, PostPageDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IPostRepository _repository = postRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public override Task<PostPageDto> Handle(CreatePostCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync("CreatePost", request, async activity =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var author = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException<string>($"Author with ID {userId} not found.");

            var post = _mapper.Map<Post>(request);
            post.Id = Guid.NewGuid();
            post.CreationDate = DateTimeOffset.UtcNow;
            post.Author = author;
            post.AuthorId = author.Id;

            await _repository.AddAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PostPageDto>(post);
        });
}
