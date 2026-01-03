// <copyright file="UnitTest1.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using FluentAssertions;

namespace PromptHub.Web.UnitTests;

/// <summary>
/// Dummy unit test class to verify test setup.
/// </summary>
public sealed class UnitTest1
{
    /// <summary>
    /// Dummy test.
    /// </summary>
    [Fact]
    public void Sanity_check_should_pass()
        => true.Should().BeTrue();
}
