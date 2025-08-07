using Application.Common.Handlers;
using Application.DTOs.Comment;
using Application.DTOs.Posts;
using Application.DTOs.Users;
using Application.Exceptions;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Application.Features.Posts.Commands;

/// <summary>
/// Обработчик создания поста.
/// </summary>
public class CreatePostCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IPostRepository postRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreatePostCommandHandler> logger,
    IMapper mapper,
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<CreatePostCommand, PostPageDto>(logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly IPostRepository _repository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private static readonly ActivitySource ActivitySource = new(nameof(CreatePostCommandHandler));

    public override async Task<PostPageDto> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("CreatePost", ActivityKind.Server);
        SetTracingTags(activity, request);

        var userId = GetCurrentUserId();
        activity?.SetTag("enduser.id", userId);

        Post post = MapToPost(request);

        ApplicationUser author;
        try
        {
            author = await FindAuthorAsync(userId, activity);
            post.Author = author;
            post.AuthorId = author.Id;
            await SavePostAsync(post, cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }

        var dto = MapToDto(post);
        LogSuccess(post, activity, stopwatch.ElapsedMilliseconds);

        stopwatch.Stop();
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);
        activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

        return dto;
    }

    private string GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new UnauthorizedAccessException("HTTP context is missing.");

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("An invalid user ID was received from claims.");
        return userId;
    }

    private async Task<ApplicationUser> FindAuthorAsync(string authorId, Activity? activity)
    {
        var author = await _userManager.FindByIdAsync(authorId);
        if (author == null)
        {
            var msg = $"Author with ID {authorId} not found.";
            _logger.LogWarning(msg);
            activity?.SetStatus(ActivityStatusCode.Error, "Find user failed");
            throw new NotFoundException<string>(msg);
        }
        return author;
    }

    private Post MapToPost(CreatePostCommand request)
    {
        var post = _mapper.Map<Post>(request);
        post.Id = Guid.NewGuid();
        post.CreationDate = DateTimeOffset.UtcNow;
        return post;
    }

    private PostPageDto MapToDto(Post post)
    {
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

        return dto;
    }

    private async Task SavePostAsync(Post post, CancellationToken cancellationToken)
    {
        await _repository.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void LogSuccess(Post post, Activity? activity, long? durationMs = null)
    {
        _logger.LogInformation("Post created: {PostId}, Length: {Length}, AuthorId: {AuthorId}, HasComments: {HasComments}",
            post.Id, post.Content?.Length ?? 0, post.AuthorId, post.Comments?.Any() == true);

        activity?.SetTag("post.id", post.Id);
        activity?.SetTag("post.content_length", post.Content?.Length ?? 0);
        activity?.SetTag("post.author_id", post.AuthorId);
        activity?.SetTag("post.has_comments", post.Comments?.Any() == true);
        if (durationMs != null)
            activity?.SetTag("operation.duration_ms", durationMs.Value);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("PostCreated"));
    }
}
