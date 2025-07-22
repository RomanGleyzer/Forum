using Application.DTOs.Comment;
using Application.DTOs.Posts;
using Application.DTOs.Users;
using Application.Exceptions;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Application.Features.Posts.Commands;

public class CreatePostCommandHandler(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IPostReadModelRepository postReadModelRepository,
    IPostRepository postRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreatePostCommandHandler> logger,
    IMapper mapper) : IRequestHandler<CreatePostCommand, PostPageDto>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IPostReadModelRepository _postReadModelRepository = postReadModelRepository;
    private readonly IPostRepository _repository = postRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CreatePostCommandHandler> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private static readonly ActivitySource ActivitySource = new(nameof(CreatePostCommandHandler));

    public async Task<PostPageDto> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        using var activity = StartActivity("CreatePost", request);

        try
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            activity?.SetTag("author.id", userId);
            activity?.SetTag("operation", "get-current-user");

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("An invalid user ID was received when trying to retrieve an ID from claims.");

            var author = await FindAuthorAsync(userId, activity);
            var post = MapToPost(request, author);
          
            await SavePostAsync(post, cancellationToken);

            var dto = new PostPageDto
            {
                Id = post.Id,
                Content = post.Content,
                CreationDate = post.CreationDate,
                Author = new AuthorDto
                {
                    Id = post.Author.Id,
                    FirstName = post.Author.FirstName,
                    LastName = post.Author.LastName
                },
                FeaturedComment = post.Comments
                .OrderByDescending(c => c.CreationDate)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    Author = new AuthorDto
                    {
                        Id = c.Author.Id,
                        FirstName = c.Author.FirstName,
                        LastName = c.Author.LastName,
                    },
                    CreationDate = c.CreationDate
                })
                .FirstOrDefault()
            };

            LogSuccess(post, activity);

            return dto;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
    }

    private Activity? StartActivity(string name, CreatePostCommand request)
    {
        var activity = ActivitySource.StartActivity(name);
        if (activity == null)
            _logger.LogWarning("Tracing is not enabled. Activity is null for {Handler}.", name);

        return activity;
    }

    private async Task<ApplicationUser> FindAuthorAsync(string authorId, Activity? activity)
    {
        var author = await _userManager.FindByIdAsync(authorId.ToString());
        if (author == null)
        {
            var msg = $"Author with ID {authorId} not found.";
            _logger.LogWarning(msg);
            activity?.SetStatus(ActivityStatusCode.Error, "Find user failed");
            throw new NotFoundException<string>(msg);
        }
        return author;
    }

    private Post MapToPost(CreatePostCommand request, ApplicationUser author)
    {
        var post = _mapper.Map<Post>(request);
        post.Id = Guid.NewGuid();
        post.AuthorId = author.Id;
        post.CreationDate = DateTimeOffset.UtcNow;
        return post;
    }

    private async Task SavePostAsync(Post post, CancellationToken cancellationToken)
    {
        await _repository.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void LogSuccess(Post post, Activity? activity)
    {
        _logger.LogInformation("Post created: {PostId}", post.Id);
        activity?.SetTag("post.id", post.Id);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("PostCreated"));
    }

    private void HandleException(Exception ex, Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace }
            }));
    }
}
