namespace Domain.Exceptions;

public class PostTooLongException(Guid postId) : Exception($"The post with ID {postId} is too long.")
{
    public Guid PostId { get; } = postId;
}
