using Application.Exceptions;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Commands;

public class CreatePostCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPostRepository postRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreatePostCommandHandler> logger,
    IMapper mapper) : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository = postRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CreatePostCommandHandler> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private static readonly ActivitySource ActivitySource = new(nameof(CreatePostCommandHandler));

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        using var activity = StartActivity("CreatePost", request);

        try
        {
            var author = await FindAuthorAsync(request.AuthorId, activity);
            var post = MapToPost(request, author);

            await SavePostAsync(post, cancellationToken);
            LogSuccess(post, activity);

            return post.Id;
        }
        catch (Exception ex)
        {
            HandleException(ex, request.AuthorId, activity);
            throw;
        }
    }

    private Activity? StartActivity(string name, CreatePostCommand request)
    {
        var activity = ActivitySource.StartActivity(name);
        if (activity == null)
            _logger.LogWarning("Tracing is not enabled. Activity is null for {Handler}.", name);

        activity?.SetTag("author.id", request.AuthorId);
        return activity;
    }

    private async Task<ApplicationUser> FindAuthorAsync(Guid authorId, Activity? activity)
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

    private void HandleException(Exception ex, Guid authorId, Activity? activity)
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

        _logger.LogError(ex, "Failed to create post for user {UserId}", authorId);
    }
}
