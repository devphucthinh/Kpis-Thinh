using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Xunit;

namespace Kpi.Web.EndToEndTests;

public sealed class KpiFullFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public KpiFullFlowTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Control_center_is_browser_navigable_with_theme_and_narrow_viewport()
    {
        var response = await client.GetAsync("/?persona=creator", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var themeScriptResponse = await client.GetAsync("/js/theme.js", TestContext.Current.CancellationToken);
        var themeScript = await themeScriptResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, themeScriptResponse.StatusCode);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 1280, Height = 900 } });
        await page.SetContentAsync(html);

        await AssertVisible(page, "[data-theme-toggle]");
        await AssertVisible(page, "main#main-content");
        Assert.Equal("control-center", await page.Locator("html").GetAttributeAsync("data-page"));

        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = themeScript });
        await page.Locator("[data-theme-toggle]").ClickAsync();
        Assert.Equal("dark", await page.Locator("html").GetAttributeAsync("data-theme"));

        await page.SetViewportSizeAsync(390, 844);
        await AssertVisible(page, "main#main-content");
        Assert.Equal(390, await page.EvaluateAsync<int>("() => window.innerWidth"));
    }

    [Fact]
    public async Task Audit_page_has_labeled_filters_and_keyboard_target()
    {
        var response = await client.GetAsync("/Audit?persona=admin", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(6, await page.Locator("form[aria-label='Bộ lọc Audit'] label").CountAsync());
        await page.Locator("form[aria-label='Bộ lọc Audit'] button[type='submit']").FocusAsync();
        Assert.Equal("submit", await page.Locator(":focus").GetAttributeAsync("type"));
    }

    private static async Task AssertVisible(IPage page, string selector)
    {
        var locator = page.Locator(selector);
        Assert.True(await locator.IsVisibleAsync(), $"Expected selector to be visible: {selector}");
    }
}
