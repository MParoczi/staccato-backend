using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class ChordRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<ChordEntity, Chord>(context, mapper), IChordRepository
{
}
