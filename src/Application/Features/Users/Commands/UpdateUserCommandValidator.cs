using Application.Features.Posts.Queries;
using FluentValidation;

namespace Application.Features.Users.Commands;

public class UpdateUserCommandValidator : AbstractValidator<GetUserPostsQuery>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0).WithMessage("Skip must be >= 0");
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100).WithMessage("Take must be between 1 and 100.");
    }
}
