using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.GetLegalDocument;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.GetLegalDocument;

public sealed class GetLegalDocumentUseCaseTests
{
    private readonly ILegalDocumentRepository _repository = Substitute.For<ILegalDocumentRepository>();
    private readonly GetLegalDocumentUseCase _sut;

    public GetLegalDocumentUseCaseTests()
    {
        _sut = new GetLegalDocumentUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnDocument_WhenTermsOfServiceExists()
    {
        var effectiveDate = new DateTimeOffset(2026, 4, 5, 0, 0, 0, TimeSpan.FromHours(9));
        var document = LegalDocument.Create(LegalDocumentType.TermsOfService, "1.0", effectiveDate, "# Terms");
        _repository.GetLatestByTypeAsync(LegalDocumentType.TermsOfService, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _sut.ExecuteAsync(LegalDocumentType.TermsOfService, CancellationToken.None);

        result.Version.Should().Be("1.0");
        result.EffectiveDate.Should().Be("2026-04-05");
        result.Content.Should().Be("# Terms");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnDocument_WhenPrivacyPolicyExists()
    {
        var effectiveDate = new DateTimeOffset(2026, 4, 5, 0, 0, 0, TimeSpan.FromHours(9));
        var document = LegalDocument.Create(LegalDocumentType.PrivacyPolicy, "2.0", effectiveDate, "# Privacy");
        _repository.GetLatestByTypeAsync(LegalDocumentType.PrivacyPolicy, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _sut.ExecuteAsync(LegalDocumentType.PrivacyPolicy, CancellationToken.None);

        result.Version.Should().Be("2.0");
        result.Content.Should().Be("# Privacy");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFoundException_WhenDocumentDoesNotExist()
    {
        _repository.GetLatestByTypeAsync(LegalDocumentType.TermsOfService, Arg.Any<CancellationToken>())
            .Returns((LegalDocument?)null);

        var act = () => _sut.ExecuteAsync(LegalDocumentType.TermsOfService, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallRepositoryWithCorrectType()
    {
        var document = LegalDocument.Create(LegalDocumentType.PrivacyPolicy, "1.0", DateTimeOffset.UtcNow, "content");
        _repository.GetLatestByTypeAsync(LegalDocumentType.PrivacyPolicy, Arg.Any<CancellationToken>())
            .Returns(document);

        await _sut.ExecuteAsync(LegalDocumentType.PrivacyPolicy, CancellationToken.None);

        await _repository.Received(1).GetLatestByTypeAsync(LegalDocumentType.PrivacyPolicy, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetLatestByTypeAsync(LegalDocumentType.TermsOfService, Arg.Any<CancellationToken>());
    }
}
