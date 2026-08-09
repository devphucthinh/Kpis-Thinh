using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kpi.Domain.Formula.Ast;

namespace Kpi.Domain.Formula.Serialization;

/// <summary>Explicit, versioned JSON contract for formula snapshots.</summary>
public static class FormulaDocumentSerializer
{
    public static string Serialize(FormulaDocument document)
    {
        var root = new JsonObject
        {
            ["source"] = document.Source,
            ["formulaLanguageVersion"] = document.LanguageVersion,
            ["astSchemaVersion"] = document.AstSchemaVersion,
            ["ast"] = WriteNode(document.Ast)
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static FormulaDocument Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var source = root.GetProperty("source").GetString() ?? string.Empty;
        var languageVersion = root.GetProperty("formulaLanguageVersion").GetInt32();
        var schemaVersion = root.GetProperty("astSchemaVersion").GetInt32();
        if (schemaVersion != 1) throw new InvalidOperationException("FORMULA_AST_SCHEMA_UNSUPPORTED");
        return new FormulaDocument(source, ReadNode(root.GetProperty("ast")), languageVersion, schemaVersion);
    }

    private static JsonObject WriteNode(FormulaNode node)
    {
        var obj = new JsonObject
        {
            ["nodeType"] = node switch
            {
                DecimalLiteralNode => "decimalLiteral",
                BooleanLiteralNode => "booleanLiteral",
                VariableNode => "variable",
                UnaryNode => "unary",
                BinaryNode => "binary",
                PercentNode => "percent",
                CallNode => "call",
                _ => throw new InvalidOperationException("FORMULA_AST_NODE_UNSUPPORTED")
            },
            ["resultType"] = node.ResultType.ToString(),
            ["span"] = new JsonObject { ["start"] = node.Span.Start, ["length"] = node.Span.Length }
        };
        switch (node)
        {
            case DecimalLiteralNode decimalLiteral: obj["value"] = decimalLiteral.Value.ToString(CultureInfo.InvariantCulture); break;
            case BooleanLiteralNode booleanLiteral: obj["value"] = booleanLiteral.Value; break;
            case VariableNode variable: obj["code"] = variable.Code; break;
            case UnaryNode unary: obj["operator"] = unary.Operator.ToString(); obj["operand"] = WriteNode(unary.Operand); break;
            case BinaryNode binary: obj["operator"] = binary.Operator.ToString(); obj["left"] = WriteNode(binary.Left); obj["right"] = WriteNode(binary.Right); break;
            case PercentNode percent: obj["operand"] = WriteNode(percent.Operand); break;
            case CallNode call: obj["name"] = call.Name; obj["arguments"] = new JsonArray(call.Arguments.Select(WriteNode).ToArray()); break;
        }
        return obj;
    }

    private static FormulaNode ReadNode(JsonElement element)
    {
        var type = element.GetProperty("nodeType").GetString();
        var spanElement = element.GetProperty("span");
        var span = new SourceSpan(spanElement.GetProperty("start").GetInt32(), spanElement.GetProperty("length").GetInt32());
        return type switch
        {
            "decimalLiteral" => new DecimalLiteralNode(decimal.Parse(element.GetProperty("value").GetString()!, CultureInfo.InvariantCulture), span),
            "booleanLiteral" => new BooleanLiteralNode(element.GetProperty("value").GetBoolean(), span),
            "variable" => new VariableNode(element.GetProperty("code").GetString()!, ParseResultType(element), span),
            "unary" => new UnaryNode(Enum.Parse<UnaryOperator>(element.GetProperty("operator").GetString()!), ReadNode(element.GetProperty("operand")), ParseResultType(element), span),
            "binary" => new BinaryNode(Enum.Parse<BinaryOperator>(element.GetProperty("operator").GetString()!), ReadNode(element.GetProperty("left")), ReadNode(element.GetProperty("right")), ParseResultType(element), span),
            "percent" => new PercentNode(ReadNode(element.GetProperty("operand")), span),
            "call" => new CallNode(element.GetProperty("name").GetString()!, element.GetProperty("arguments").EnumerateArray().Select(ReadNode).ToArray(), ParseResultType(element), span),
            _ => throw new InvalidOperationException("FORMULA_AST_NODE_UNSUPPORTED")
        };
    }

    private static FormulaResultType ParseResultType(JsonElement element) => Enum.Parse<FormulaResultType>(element.GetProperty("resultType").GetString()!, ignoreCase: true);
}
