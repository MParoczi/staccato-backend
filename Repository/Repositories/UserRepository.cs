using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class UserRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<UserEntity, User>(context, mapper), IUserRepository
{
}
