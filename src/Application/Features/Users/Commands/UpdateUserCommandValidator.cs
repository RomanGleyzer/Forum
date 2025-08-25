using Application.Features.Posts.Queries;
using FluentValidation;

namespace Application.Features.Users.Commands;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.About).MaximumLength(500);
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-120)));
    }
}