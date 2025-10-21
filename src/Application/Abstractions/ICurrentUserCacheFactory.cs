using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Abstractions;

public interface ICurrentUserCacheFactory
{
    CurrentUserDto Create(ApplicationUser user);
}