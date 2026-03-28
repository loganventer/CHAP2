using CHAP2.Application.Interfaces;
using CHAP2.Chorus.Api.Controllers;
using CHAP2.Chorus.Api.Requests;
using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using CHAP2.Shared.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CHAP2.Tests.Api;

[TestFixture]
public class ChorusesControllerTests
{
    private IChorusQueryService _queryService = null!;
    private IChorusCommandService _commandService = null!;
    private ILogger<ChorusesController> _logger = null!;
    private IOptions<SearchSettings> _searchSettings = null!;
    private ChorusesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _queryService = Substitute.For<IChorusQueryService>();
        _commandService = Substitute.For<IChorusCommandService>();
        _logger = Substitute.For<ILogger<ChorusesController>>();
        _searchSettings = Options.Create(new SearchSettings
        {
            MaxResults = 50,
            DefaultSearchMode = "Contains",
            DefaultSearchScope = "all"
        });
        _sut = new ChorusesController(_logger, _queryService, _commandService, _searchSettings);
    }

    private static ChorusEntity CreateTestChorus(Guid? id = null, string name = "Test Chorus")
    {
        return ChorusEntity.Reconstitute(
            id ?? Guid.NewGuid(), name, "Test text", MusicalKey.C, ChorusType.Praise,
            TimeSignature.FourFour, DateTime.UtcNow, null, null);
    }

    // --- AddChorus ---

    [Test]
    public async Task AddChorus_WithValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var request = new CreateChorusRequest
        {
            Name = "New Chorus",
            ChorusText = "Chorus text",
            Key = MusicalKey.C,
            Type = ChorusType.Praise,
            TimeSignature = TimeSignature.FourFour
        };

        var createdChorus = CreateTestChorus(name: "New Chorus");
        _commandService.CreateChorusAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MusicalKey>(),
                Arg.Any<ChorusType>(), Arg.Any<TimeSignature>(), Arg.Any<CancellationToken>())
            .Returns(createdChorus);

        // Act
        var result = await _sut.AddChorus(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Test]
    public async Task AddChorus_WhenChorusAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var request = new CreateChorusRequest
        {
            Name = "Existing Chorus",
            ChorusText = "Text",
            Key = MusicalKey.C,
            Type = ChorusType.Praise,
            TimeSignature = TimeSignature.FourFour
        };

        _commandService.CreateChorusAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MusicalKey>(),
                Arg.Any<ChorusType>(), Arg.Any<TimeSignature>(), Arg.Any<CancellationToken>())
            .Throws(new ChorusAlreadyExistsException("Existing Chorus"));

        // Act
        var result = await _sut.AddChorus(request);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // --- GetAllChoruses ---

    [Test]
    public async Task GetAllChoruses_ShouldReturnOkWithChoruses()
    {
        // Arrange
        var choruses = new List<ChorusEntity>
        {
            CreateTestChorus(name: "Chorus One"),
            CreateTestChorus(name: "Chorus Two")
        };
        _queryService.GetAllChorusesAsync(Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.GetAllChoruses();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(choruses);
    }

    [Test]
    public async Task GetAllChoruses_WhenEmpty_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        _queryService.GetAllChorusesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ChorusEntity>());

        // Act
        var result = await _sut.GetAllChoruses();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // --- GetChorusById ---

    [Test]
    public async Task GetChorusById_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var chorus = CreateTestChorus(id);
        _queryService.GetChorusByIdAsync(id, Arg.Any<CancellationToken>()).Returns(chorus);

        // Act
        var result = await _sut.GetChorusById(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var returnedChorus = okResult.Value as ChorusEntity;
        returnedChorus.Should().NotBeNull();
        returnedChorus!.Id.Should().Be(id);
    }

    [Test]
    public async Task GetChorusById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _queryService.GetChorusByIdAsync(id, Arg.Any<CancellationToken>())
            .Throws(new ChorusNotFoundException(id));

        // Act
        var result = await _sut.GetChorusById(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- SearchChoruses ---

    [Test]
    public async Task SearchChoruses_WithValidQuery_ShouldReturnOk()
    {
        // Arrange
        var choruses = new List<ChorusEntity> { CreateTestChorus(name: "Amazing Grace") };
        _queryService.SearchChorusesAsync("amazing", Arg.Any<SearchMode>(), Arg.Any<SearchScope>(), Arg.Any<CancellationToken>())
            .Returns(choruses);

        // Act
        var result = await _sut.SearchChoruses("amazing");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task SearchChoruses_WithEmptyQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _sut.SearchChoruses(q: "");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task SearchChoruses_WithNullQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _sut.SearchChoruses(q: null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --- DeleteChorus ---

    [Test]
    public async Task DeleteChorus_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteChorus(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _commandService.Received(1).DeleteChorusAsync(id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteChorus_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _commandService.DeleteChorusAsync(id, Arg.Any<CancellationToken>())
            .Throws(new ChorusNotFoundException(id));

        // Act
        var result = await _sut.DeleteChorus(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
