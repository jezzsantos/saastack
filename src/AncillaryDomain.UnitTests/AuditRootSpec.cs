using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace AncillaryDomain.UnitTests;

[Trait("Category", "Unit")]
public class AuditRootSpec
{
    private readonly AuditRoot _audit;

    public AuditRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _audit = AuditRoot.Create(recorder.Object, identifierFactory.Object,
            "anagainstid".ToId(), "atenantid".ToId(), "anauditcode", "amessagetemplate",
            TemplateArguments.Create(["anarg1", "anarg2"]).Value).Value;
    }

    [Fact]
    public void WhenCreate_ThenComplete()
    {
        _audit.AuditCode.Should().BeSome("anauditcode");
        _audit.AgainstId.Should().BeSome("anagainstid".ToId());
        _audit.OrganizationId.Should().BeSome("atenantid".ToId());
        _audit.MessageTemplate.Should().BeSome("amessagetemplate");
        _audit.TemplateArguments.Value.Items.Count.Should().Be(2);
        _audit.TemplateArguments.Value.Items[0].Should().Be("anarg1");
        _audit.TemplateArguments.Value.Items[1].Should().Be("anarg2");
    }
}