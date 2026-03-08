using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class LessonPageRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<LessonPageEntity, LessonPage>(context, mapper), ILessonPageRepository
{
}
