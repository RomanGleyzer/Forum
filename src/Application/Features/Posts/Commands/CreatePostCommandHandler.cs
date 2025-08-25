using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Features.Posts.Commands;

public sealed class CreatePostCommandHandler(
    ILogger<CreatePostCommandHandler> logger,
    ICurrentUserService currentUser,
    IMapper mapper,
    IPostRepository repository,
    IUnitOfWork unitOfWork)
    : RequestHandlerBase<CreatePostCommand, Guid>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IPostRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public override Task<Guid> Handle(CreatePostCommand request, CancellationToken ct) =>
        ExecuteAsync("CreatePost", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var post = _mapper.Map<Post>(request);
            post.Id = Guid.NewGuid();
            post.CreationDate = DateTimeOffset.UtcNow;
            post.AuthorId = userId;

            await _repository.AddAsync(post, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            activity?.SetTag("post.id", post.Id);

            return post.Id;
        });
}
