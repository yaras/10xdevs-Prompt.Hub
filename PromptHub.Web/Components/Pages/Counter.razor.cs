// <copyright file="Counter.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace PromptHub.Web.Components.Pages;

/// <summary>
/// Provides a simple counter demo page.
/// </summary>
public partial class Counter : ComponentBase
{
    /// <summary>
    /// Gets the current count value.
    /// </summary>
    protected int CurrentCount { get; private set; }

    /// <summary>
    /// Increments the current count value.
    /// </summary>
    protected void IncrementCount()
    {
        this.CurrentCount++;
    }
}
