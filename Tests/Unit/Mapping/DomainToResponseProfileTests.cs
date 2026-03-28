using Api.Mapping;
using ApiModels.Users;
using AutoMapper;
using DomainModels.Enums;
using DomainModels.Models;
using Repository.Mapping;

namespace Tests.Unit.Mapping;

public class DomainToResponseProfileTests
{
    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityToDomainProfile>();
            cfg.AddProfile<DomainToResponseProfile>();
        }).CreateMapper();
    }

    [Fact]
    public void UserToUserResponse_EnglishLanguage_MapsToEn()
    {
        var mapper = CreateMapper();
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "a@a.com",
            FirstName = "F", LastName = "L",
            Language = Language.English
        };

        var response = mapper.Map<UserResponse>(user);

        Assert.Equal("en", response.Language);
    }

    [Fact]
    public void UserToUserResponse_HungarianLanguage_MapsToHu()
    {
        var mapper = CreateMapper();
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "a@a.com",
            FirstName = "F", LastName = "L",
            Language = Language.Hungarian
        };

        var response = mapper.Map<UserResponse>(user);

        Assert.Equal("hu", response.Language);
    }

    [Fact]
    public void UserSavedPresetToPresetResponse_DeserializesStyles()
    {
        var mapper = CreateMapper();
        var stylesJson = """[{"ModuleType":"Title","StylesJson":"{}"}]""";
        var preset = new UserSavedPreset { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Test", StylesJson = stylesJson };

        var response = mapper.Map<PresetResponse>(preset);

        Assert.Equal("Test", response.Name);
        Assert.Single(response.Styles);
        Assert.Equal("Title", response.Styles[0].ModuleType);
    }
}