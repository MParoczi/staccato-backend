using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class UserSavedPresetRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<UserSavedPresetEntity, UserSavedPreset>(context, mapper), IUserSavedPresetRepository
{
}
