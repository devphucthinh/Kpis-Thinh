using System.Net;
using System.Text.RegularExpressions;
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

    [Fact]
    public async Task Workbench_suggests_round_syntax_and_inserts_function_with_keyboard()
    {
        var index = await client.GetStringAsync("/Kpis?persona=creator", TestContext.Current.CancellationToken);
        var definitionId = Regex.Match(index, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        Assert.NotEmpty(definitionId);
        var editResponse = await client.GetAsync($"/Kpis/Edit/{definitionId}?persona=creator", TestContext.Current.CancellationToken);
        var html = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var editorScript = await client.GetStringAsync("/js/formula-editor.js", TestContext.Current.CancellationToken);
        var catalogJson = await client.GetStringAsync("/api/v1/formulas/capabilities?persona=creator", TestContext.Current.CancellationToken);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 390, Height = 844 } });
        await page.SetContentAsync(html);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = editorScript.Replace("export function ", "function ", StringComparison.Ordinal) });
        await page.EvaluateAsync("catalogJson => { const catalog = JSON.parse(catalogJson); window.fetch = async url => { const text = String(url).includes('capabilities') ? JSON.stringify(catalog) : JSON.stringify({ diagnostics: [], formula: null }); return new Response(text, { status: 200, headers: { 'Content-Type': 'application/json' } }); }; attachFormulaEditor(document.getElementById('formula-source'), document.getElementById('formula-variables'), document.getElementById('formula-diagnostics'), document.getElementById('formula-ast'), document.getElementById('formula-test-inputs'), document.getElementById('formula-test-button'), document.getElementById('formula-test-result'), document.getElementById('formula-variable-rows'), document.getElementById('formula-variables-json'), document.getElementById('formula-suggestions-panel'), document.getElementById('formula-syntax-helper')); }", catalogJson);

        var source = page.Locator("#formula-source");
        await source.FillAsync("RO");
        var option = page.Locator("#formula-suggestions-panel [role='option']").Filter(new LocatorFilterOptions { HasText = "ROUND" }).First;
        await option.WaitForAsync();
        Assert.Contains("ROUND(value, decimals)", await page.Locator("#formula-syntax-helper").InnerTextAsync(), StringComparison.Ordinal);
        Assert.Contains("Ví dụ:", await page.Locator("#formula-syntax-helper").InnerTextAsync(), StringComparison.Ordinal);
        await source.PressAsync("Enter");
        Assert.Equal("ROUND()", await source.InputValueAsync());
        Assert.Equal(390, await page.EvaluateAsync<int>("() => window.innerWidth"));
    }

    private static async Task AssertVisible(IPage page, string selector)
    {
        var locator = page.Locator(selector);
        Assert.True(await locator.IsVisibleAsync(), $"Expected selector to be visible: {selector}");
    }
}
