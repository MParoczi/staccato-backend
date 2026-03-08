using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class ModuleRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<ModuleEntity, Module>(context, mapper), IModuleRepository
{
}
