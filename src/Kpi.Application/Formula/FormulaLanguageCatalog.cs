namespace Kpi.Application.Formula;

/// <summary>Public, presentation-neutral description of the closed formula language.</summary>
public static class FormulaLanguageCatalog
{
    public const int FormulaLanguageVersion = 1;
    public const int AstSchemaVersion = 1;

    public static IReadOnlyList<FormulaOperationDescriptor> Operators { get; } =
    [
        Operation("+", "operator", "left + right", ["left", "right"], "Cộng hai giá trị Decimal.", "revenue + bonus", " + "),
        Operation("-", "operator", "left - right", ["left", "right"], "Trừ hai giá trị Decimal.", "revenue - discount", " - "),
        Operation("*", "operator", "left * right", ["left", "right"], "Nhân hai giá trị Decimal.", "units * price", " * "),
        Operation("/", "operator", "left / right", ["left", "right"], "Chia hai giá trị Decimal; chia cho 0 trả lỗi.", "revenue / target", " / "),
        Operation("%", "operator", "value%", ["value"], "Chuyển Decimal thành phần trăm theo nghĩa value / 100.", "discount%", "%"),
        Operation("MOD", "operator", "left MOD right", ["left", "right"], "Lấy phần dư của hai giá trị Decimal.", "worked MOD 7", " MOD "),
        Operation("=", "operator", "left = right", ["left", "right"], "So sánh bằng.", "status = 1", " = "),
        Operation("!=", "operator", "left != right", ["left", "right"], "So sánh khác.", "status != 0", " != "),
        Operation("<", "operator", "left < right", ["left", "right"], "So sánh nhỏ hơn.", "actual < target", " < "),
        Operation("<=", "operator", "left <= right", ["left", "right"], "So sánh nhỏ hơn hoặc bằng.", "actual <= target", " <= "),
        Operation(">", "operator", "left > right", ["left", "right"], "So sánh lớn hơn.", "actual > target", " > "),
        Operation(">=", "operator", "left >= right", ["left", "right"], "So sánh lớn hơn hoặc bằng.", "actual >= target", " >= "),
        Operation("AND", "operator", "left AND right", ["left", "right"], "AND logic giữa hai Boolean.", "active AND approved", " AND "),
        Operation("OR", "operator", "left OR right", ["left", "right"], "OR logic giữa hai Boolean.", "manual OR automatic", " OR "),
        Operation("NOT", "operator", "NOT value", ["value"], "Đảo giá trị Boolean.", "NOT archived", "NOT ")
    ];

    public static IReadOnlyList<FormulaOperationDescriptor> Functions { get; } =
    [
        Operation("IF", "function", "IF(condition, whenTrue, whenFalse)", ["condition", "whenTrue", "whenFalse"], "Trả về một trong hai nhánh theo điều kiện Boolean.", "IF(active, revenue, 0)", "IF(condition, whenTrue, whenFalse)"),
        Operation("ROUND", "function", "ROUND(value, decimals)", ["value", "decimals"], "Làm tròn Decimal; decimals từ 0 đến 10.", "ROUND(revenue / target * 100, 2)", "ROUND(value, decimals)"),
        Operation("ABS", "function", "ABS(value)", ["value"], "Lấy giá trị tuyệt đối của Decimal.", "ABS(actual - target)", "ABS(value)"),
        Operation("MOD", "function", "MOD(left, right)", ["left", "right"], "Dạng hàm của phép lấy phần dư Decimal.", "MOD(worked, 7)", "MOD(left, right)")
    ];

    public static IReadOnlyList<FormulaOperationDescriptor> All => Operators.Concat(Functions).ToArray();

    public static FormulaLanguageContract ToContract() => new(
        FormulaLanguageVersion,
        AstSchemaVersion,
        Operators,
        Functions,
        All.Select(operation => operation.Example).ToArray());

    private static FormulaOperationDescriptor Operation(string name, string kind, string signature, IReadOnlyList<string> parameters, string description, string example, string insertText) =>
        new(name, kind, signature, parameters, description, example, insertText);
}

public sealed record FormulaOperationDescriptor(
    string Name,
    string Kind,
    string Signature,
    IReadOnlyList<string> Parameters,
    string Description,
    string Example,
    string InsertText);

public sealed record FormulaLanguageContract(
    int FormulaLanguageVersion,
    int AstSchemaVersion,
    IReadOnlyList<FormulaOperationDescriptor> Operators,
    IReadOnlyList<FormulaOperationDescriptor> Functions,
    IReadOnlyList<string> Examples);
