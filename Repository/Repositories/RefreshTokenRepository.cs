using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class RefreshTokenRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<RefreshTokenEntity, RefreshToken>(context, mapper), IRefreshTokenRepository
{
}
