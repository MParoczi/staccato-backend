using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class NotebookModuleStyleRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<NotebookModuleStyleEntity, NotebookModuleStyle>(context, mapper), INotebookModuleStyleRepository
{
}
