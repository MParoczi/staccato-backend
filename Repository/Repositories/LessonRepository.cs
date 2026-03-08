using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class LessonRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<LessonEntity, Lesson>(context, mapper), ILessonRepository
{
}
