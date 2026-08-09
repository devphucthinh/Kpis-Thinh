export function insertAtSelection(textarea, text) {
  const start = textarea.selectionStart;
  const end = textarea.selectionEnd;
  textarea.setRangeText(text, start, end, "end");
  textarea.dispatchEvent(new Event("input", { bubbles: true }));
}

export function parseVariableCodes(text) {
  return text
    .split(/[\r\n,]/)
    .map(code => code.trim())
    .filter(Boolean)
    .map(code => ({ code, displayName: code, type: "Decimal", required: true, description: null }));
}

export function attachFormulaEditor(source, variableInput, diagnostics, astPreview) {
  let timer;
  const scheduleValidation = () => {
    window.clearTimeout(timer);
    timer = window.setTimeout(async () => {
      const response = await fetch("/api/v1/formulas/validate", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ source: source.value, declaredResultType: "Decimal", variables: parseVariableCodes(variableInput.value) })
      });
      const data = await response.json();
      diagnostics.textContent = (data.diagnostics || []).map(d => `${d.code}: ${d.message}`).join("\n");
      astPreview.textContent = data.formula ? JSON.stringify(data.formula.ast, null, 2) : "";
    }, 250);
  };
  source.addEventListener("input", scheduleValidation);
  variableInput.addEventListener("input", scheduleValidation);
}
