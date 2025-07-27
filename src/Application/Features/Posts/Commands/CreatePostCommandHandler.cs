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

public class CreatePostCommandHandler(IHttpContextAccessor httpContextAccessor, IPostRepository postRepository, IUnitOfWork unitOfWork, ILogger<CreatePostCommandHandler> logger, IMapper mapper, UserManager<ApplicationUser> userManager) 
    : QueryHandlerBase<CreatePostCommand, PostPageDto>(logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IPostRepository _repository = postRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private static readonly ActivitySource ActivitySource = new(nameof(CreatePostCommandHandler));

    public override async Task<PostPageDto> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("CreatePost", ActivityKind.Server);
        SetTracingTags(activity, request);

        var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        activity?.SetTag("enduser.id", userId);

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("An invalid user ID was received when trying to retrieve an ID from claims.");

        var post = MapToPost(request, null!);

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
        LogSuccess(post, activity, sw.ElapsedMilliseconds);

        sw.Stop();
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);
        activity?.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);

        return dto;
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

    private void HandleException(Exception ex, Activity? activity, CreatePostCommand? request = null)
    {
        _logger.LogError(ex, "Error creating post: {Message}", ex.Message);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace ?? "" },
                { "request.body", request != null ? System.Text.Json.JsonSerializer.Serialize(request) : string.Empty }
            }));
    }
}
