export function insertAtSelection(textarea, text) {
  const start = textarea.selectionStart;
  const end = textarea.selectionEnd;
  textarea.setRangeText(text, start, end, "end");
  textarea.dispatchEvent(new Event("input", { bubbles: true }));
}

export function attachFormulaEditor(source, diagnostics, astPreview) {
  let timer;
  source.addEventListener("input", () => {
    window.clearTimeout(timer);
    timer = window.setTimeout(async () => {
      const response = await fetch("/api/v1/formulas/validate", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ source: source.value, declaredResultType: "Decimal", variables: [] })
      });
      const data = await response.json();
      diagnostics.textContent = (data.diagnostics || []).map(d => `${d.code}: ${d.message}`).join("\n");
      astPreview.textContent = data.formula ? JSON.stringify(data.formula.ast, null, 2) : "";
    }, 250);
  });
}
