using FluentValidation;

namespace Application.Features.Posts.Queries;

public class GetPostByIdQueryHandlerValidator : AbstractValidator<GetPostByIdQuery>
{
    public GetPostByIdQueryHandlerValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("Post ID is required.");
    }
}
