// <copyright file="Weather.razor.cs" company="PromptHub">
// Copyright (c) PromptHub. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;

namespace PromptHub.Web.Components.Pages;

[Authorize]
/// <summary>
/// Displays a simple weather forecast table.
/// </summary>
public partial class Weather : ComponentBase
{
    /// <summary>
    /// Gets the loaded forecasts.
    /// </summary>
    protected WeatherForecast[]? Forecasts { get; private set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(500);

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var summaries = new[]
        {
            "Freezing",
            "Bracing",
            "Chilly",
            "Cool",
            "Mild",
            "Warm",
            "Balmy",
            "Hot",
            "Sweltering",
            "Scorching",
        };

        this.Forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)],
        }).ToArray();
    }

    /// <summary>
    /// Represents a single forecast item.
    /// </summary>
    protected sealed class WeatherForecast
    {
        /// <summary>
        /// Gets or sets the forecast date.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Gets or sets the temperature in Celsius.
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Gets or sets the summary text.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets the temperature in Fahrenheit.
        /// </summary>
        public int TemperatureF => 32 + (int)(this.TemperatureC / 0.5556);
    }
}
