using FluentAssertions;

namespace PromptHub.Web.UnitTests;

public sealed class UnitTest1
{
    [Fact]
    public void Sanity_check_should_pass()
        => true.Should().BeTrue();
}
