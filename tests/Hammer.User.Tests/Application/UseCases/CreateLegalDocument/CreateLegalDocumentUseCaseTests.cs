using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.CreateLegalDocument;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.CreateLegalDocument;

public sealed class CreateLegalDocumentUseCaseTests
{
    private readonly ILegalDocumentRepository _repository = Substitute.For<ILegalDocumentRepository>();
    private readonly CreateLegalDocumentUseCase _sut;

    public CreateLegalDocumentUseCaseTests()
    {
        _sut = new CreateLegalDocumentUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateDocument_WhenVersionIsNew()
    {
        var effectiveDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.FromHours(9));
        var request = new CreateLegalDocumentRequest(LegalDocumentType.TermsOfService, "2.0", effectiveDate, "# New Terms");

        _repository.GetByTypeAndVersionAsync(LegalDocumentType.TermsOfService, "2.0", Arg.Any<CancellationToken>())
            .Returns((LegalDocument?)null);

        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        result.Version.Should().Be("2.0");
        result.EffectiveDate.Should().Be("2026-05-01");
        result.Content.Should().Be("# New Terms");
        await _repository.Received(1).AddAsync(Arg.Any<LegalDocument>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowConflictException_WhenVersionAlreadyExists()
    {
        var existing = LegalDocument.Create(LegalDocumentType.TermsOfService, "1.0", DateTimeOffset.UtcNow, "existing");
        _repository.GetByTypeAndVersionAsync(LegalDocumentType.TermsOfService, "1.0", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new CreateLegalDocumentRequest(LegalDocumentType.TermsOfService, "1.0", DateTimeOffset.UtcNow, "# Duplicate");

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<LegalDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAllowSameVersionForDifferentTypes()
    {
        _repository.GetByTypeAndVersionAsync(LegalDocumentType.PrivacyPolicy, "1.0", Arg.Any<CancellationToken>())
            .Returns((LegalDocument?)null);

        var request = new CreateLegalDocumentRequest(LegalDocumentType.PrivacyPolicy, "1.0", DateTimeOffset.UtcNow, "# Privacy v1");

        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        result.Version.Should().Be("1.0");
        await _repository.Received(1).AddAsync(Arg.Any<LegalDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var act = () => _sut.ExecuteAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
