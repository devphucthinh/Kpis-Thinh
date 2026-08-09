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

export function attachFormulaEditor(source, variableInput, diagnostics, astPreview, testInputs, testButton, testResult) {
  let timer;
  const syncTestInputs = () => {
    const previous = new Map(Array.from(testInputs.querySelectorAll("[data-formula-input]")).map(input => [input.dataset.formulaInput, input.value]));
    testInputs.replaceChildren();
    for (const variable of parseVariableCodes(variableInput.value)) {
      const label = document.createElement("label");
      label.textContent = `${variable.code}: `;
      const input = document.createElement("input");
      input.type = "text";
      input.inputMode = "decimal";
      input.dataset.formulaInput = variable.code;
      input.placeholder = "Giá trị Decimal";
      input.value = previous.get(variable.code) || "";
      label.append(input);
      testInputs.append(label);
    }
  };
  const showTestResult = data => {
    if (data.outcome?.kind === "Success") {
      testResult.textContent = `Kết quả: ${data.outcome.value}\nKhông lưu Evaluation: ${data.persisted === false ? "Có" : "Không"}`;
      return;
    }
    const failure = data.outcome || data;
    testResult.textContent = `Test Run lỗi: ${failure.code || "FORMULA_INVALID"} — ${failure.message || "Công thức không hợp lệ."}`;
  };
  const runTest = async () => {
    const variables = parseVariableCodes(variableInput.value);
    const inputs = {};
    for (const variable of variables) {
      const input = testInputs.querySelector(`[data-formula-input="${variable.code}"]`);
      if (input?.value.trim()) inputs[variable.code] = input.value.trim();
    }
    testResult.textContent = "Đang chạy Test Run...";
    const response = await fetch("/api/v1/formulas/test-run", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source: source.value, declaredResultType: "Decimal", variables, inputs })
    });
    const data = await response.json();
    showTestResult(data);
  };
  const scheduleValidation = () => {
    syncTestInputs();
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
  testButton.addEventListener("click", runTest);
  syncTestInputs();
  scheduleValidation();
}
