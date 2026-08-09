namespace Kpi.Domain.Formula.Serialization;

/// <summary>Named seam for the explicit Formula AST JSON contract.</summary>
public sealed class FormulaAstJsonConverter
{
    public static string Serialize(FormulaDocument document) => FormulaDocumentSerializer.Serialize(document);
    public static FormulaDocument Deserialize(string json) => FormulaDocumentSerializer.Deserialize(json);
}
