using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Web.Queries;
using Kpi.Web.ViewModels;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiReadModelContractTests
{
    [Fact]
    public void Workbench_projection_preserves_source_ordered_variables_and_ast()
    {
        var store = new InMemoryKpiStore();
        var operations = new KpiOperations(store, new FixedClock());
        var actor = ActorContext.Demo("creator");
        var definition = AssertSuccess(operations.CreateDefinition(actor, "ROUND_TRIP", "Round trip", "Read model contract."));
        var variables = new[]
        {
            FormulaVariableDefinition.Create("gross_revenue", "Gross revenue", FormulaValueType.Decimal, displayOrder: 0),
            FormulaVariableDefinition.Create("discount", "Discount", FormulaValueType.Decimal, required: false, defaultValue: FormulaValue.Decimal(0m), displayOrder: 1)
        };
        AssertSuccess(operations.CreateVersion(actor, definition.Id, "Version 1", "First version", "gross_revenue - discount", variables, FormulaResultType.Decimal, "Initial"));

        var page = new KpiWebReadModelService(operations).GetWorkbench(actor, definition.Id);

        Assert.NotNull(page);
        var draft = page.Draft;
        Assert.NotNull(draft);
        Assert.Equal("gross_revenue - discount", draft!.Source);
        Assert.Equal(["gross_revenue", "discount"], draft.Variables.Select(x => x.Code));
        Assert.Equal("Discount", draft.Variables[1].DisplayName);
        Assert.Equal("0", draft.Variables[1].DefaultValue);
        Assert.Contains("ast", draft.AstJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Typed_input_parser_returns_stable_error_for_invalid_decimal()
    {
        var result = FormulaInputParser.Parse(
            [new("amount", "Amount", FormulaValueType.Decimal, true, null, null, 0)],
            new Dictionary<string, string> { ["amount"] = "not-a-decimal" });

        Assert.False(result.IsSuccess);
        Assert.Equal("FORMULA_INPUT_INVALID", result.Error!.Code);
    }

    [Fact]
    public void Typed_input_parser_preserves_decimal_boolean_and_null_values()
    {
        var variables = new[]
        {
            new FormulaVariableInputVm("amount", "Amount", FormulaValueType.Decimal, true, null, null, 0),
            new FormulaVariableInputVm("enabled", "Enabled", FormulaValueType.Boolean, true, null, null, 1),
            new FormulaVariableInputVm("note", "Note", FormulaValueType.Decimal, false, null, null, 2)
        };

        var result = FormulaInputParser.Parse(variables, new Dictionary<string, string>
        {
            ["amount"] = "12.50",
            ["enabled"] = "true",
            ["note"] = ""
        });

        Assert.True(result.IsSuccess);
        Assert.IsType<DecimalFormulaValue>(result.Value!["amount"]);
        Assert.Equal(12.50m, ((DecimalFormulaValue)result.Value["amount"]).Value);
        Assert.Equal(new BooleanFormulaValue(true), result.Value["enabled"]);
        Assert.Same(FormulaValue.Null, result.Value["note"]);
    }

    private static T AssertSuccess<T>(ApplicationResult<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);
    }
}
