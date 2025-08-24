using Application.Common.Files;
using MediatR;

namespace Application.Features.Users.Commands;

public record UploadUserAvatarCommand(UploadedFile File) : IRequest<string>;