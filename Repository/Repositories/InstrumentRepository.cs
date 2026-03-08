using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class InstrumentRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<InstrumentEntity, Instrument>(context, mapper), IInstrumentRepository
{
}
