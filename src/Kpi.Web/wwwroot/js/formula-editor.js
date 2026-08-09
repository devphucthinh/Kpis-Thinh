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
    .map((code, index) => ({ code, displayName: code, type: "Decimal", required: true, defaultValue: null, description: null, displayOrder: index }));
}

export function parseVariableRows(variableInput, variablesJson) {
  if (variablesJson?.value?.trim()) {
    try {
      const rows = JSON.parse(variablesJson.value);
      if (Array.isArray(rows) && rows.length > 0) return rows.sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0));
    } catch { /* fall back to the legacy newline editor */ }
  }
  return parseVariableCodes(variableInput?.value || "");
}

const functionSuggestions = ["AND", "OR", "NOT", "IF", "ROUND", "ABS", "MOD", "%"];

export function attachFormulaEditor(source, variableInput, diagnostics, astPreview, testInputs, testButton, testResult, variableRows = null, variablesJson = null) {
  if (!source || !variableInput || !diagnostics || !astPreview || !testInputs || !testButton || !testResult) return;
  let timer;
  let rows = parseVariableRows(variableInput, variablesJson);

  const syncLegacyFields = () => {
    variableInput.value = rows.map(row => row.code).join("\n");
    if (variablesJson) variablesJson.value = JSON.stringify(rows);
  };

  const readRowsFromDom = () => {
    if (!variableRows) return parseVariableRows(variableInput, variablesJson);
    return Array.from(variableRows.querySelectorAll("[data-variable-row]")).map((row, index) => ({
      code: row.querySelector("[data-variable-field='code']")?.value.trim() || "",
      displayName: row.querySelector("[data-variable-field='displayName']")?.value.trim() || "",
      type: row.querySelector("[data-variable-field='type']")?.value || "Decimal",
      required: row.querySelector("[data-variable-field='required']")?.checked ?? true,
      defaultValue: row.querySelector("[data-variable-field='defaultValue']")?.value.trim() || null,
      description: row.querySelector("[data-variable-field='description']")?.value.trim() || null,
      displayOrder: index
    })).filter(row => row.code);
  };

  const renderRows = () => {
    if (!variableRows) return;
    variableRows.replaceChildren();
    rows.forEach((row, index) => {
      const card = document.createElement("div");
      card.className = "panel formula-variable-row";
      card.dataset.variableRow = "true";
      card.innerHTML = `
        <div style="display:grid;grid-template-columns:1.2fr 1.2fr .8fr;gap:8px;align-items:end;">
          <label>Code<input data-variable-field="code" value="" autocomplete="off"></label>
          <label>Tên hiển thị<input data-variable-field="displayName" value=""></label>
          <label>Kiểu<select data-variable-field="type"><option value="Decimal">Decimal</option><option value="Boolean">Boolean</option></select></label>
        </div>
        <div style="display:grid;grid-template-columns:auto 1fr 2fr;gap:8px;align-items:end;">
          <label><input data-variable-field="required" type="checkbox"> Bắt buộc</label>
          <label>Default<input data-variable-field="defaultValue" value=""></label>
          <label>Mô tả<input data-variable-field="description" value=""></label>
        </div>`;
      card.querySelector("[data-variable-field='code']").value = row.code || "";
      card.querySelector("[data-variable-field='displayName']").value = row.displayName || row.code || "";
      card.querySelector("[data-variable-field='type']").value = row.type || "Decimal";
      card.querySelector("[data-variable-field='required']").checked = row.required !== false;
      card.querySelector("[data-variable-field='defaultValue']").value = row.defaultValue || "";
      card.querySelector("[data-variable-field='description']").value = row.description || "";
      variableRows.append(card);
    });
  };

  const syncRows = () => {
    rows = readRowsFromDom();
    syncLegacyFields();
    scheduleValidation();
  };

  const syncTestInputs = () => {
    const previous = new Map(Array.from(testInputs.querySelectorAll("[data-formula-input]")).map(input => [input.dataset.formulaInput, input.value]));
    testInputs.replaceChildren();
    for (const variable of rows) {
      const label = document.createElement("label");
      label.textContent = `${variable.displayName || variable.code} (${variable.type}): `;
      const input = document.createElement("input");
      input.type = "text";
      input.inputMode = variable.type === "Decimal" ? "decimal" : "text";
      input.dataset.formulaInput = variable.code;
      input.placeholder = variable.defaultValue || `Giá trị ${variable.type}`;
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
    rows = readRowsFromDom();
    syncLegacyFields();
    const inputs = {};
    for (const variable of rows) {
      const input = testInputs.querySelector(`[data-formula-input="${variable.code}"]`);
      if (input?.value.trim()) inputs[variable.code] = input.value.trim();
    }
    testResult.textContent = "Đang chạy Test Run...";
    const response = await fetch("/api/v1/formulas/test-run", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source: source.value, declaredResultType: "Decimal", variables: rows, inputs })
    });
    showTestResult(await response.json());
  };

  const scheduleValidation = () => {
    syncTestInputs();
    window.clearTimeout(timer);
    timer = window.setTimeout(async () => {
      const declaredVariables = rows.length > 0 ? rows : parseVariableCodes(variableInput.value);
      const legacyPayload = { variables: parseVariableCodes(variableInput.value) };
      const response = await fetch("/api/v1/formulas/validate", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ source: source.value, declaredResultType: "Decimal", variables: declaredVariables.length > 0 ? declaredVariables : legacyPayload.variables })
      });
      const data = await response.json();
      diagnostics.textContent = (data.diagnostics || []).map(d => `${d.code}: ${d.message}`).join("\n");
      astPreview.textContent = data.formula ? JSON.stringify(data.formula.ast, null, 2) : "";
    }, 250);
  };

  source.setAttribute("list", "formula-suggestions");
  source.setAttribute("autocomplete", "off");
  source.dataset.autocomplete = functionSuggestions.join(",");
  variableInput.addEventListener("input", () => { rows = parseVariableCodes(variableInput.value); renderRows(); syncTestInputs(); scheduleValidation(); });
  source.addEventListener("input", scheduleValidation);
  variableRows?.addEventListener("input", syncRows);
  document.getElementById("formula-add-variable")?.addEventListener("click", () => {
    rows = [...readRowsFromDom(), { code: "", displayName: "", type: "Decimal", required: true, defaultValue: null, description: null, displayOrder: rows.length }];
    renderRows();
    variableRows?.querySelector("[data-variable-row]:last-child [data-variable-field='code']")?.focus();
  });
  testButton.addEventListener("click", runTest);
  renderRows();
  syncLegacyFields();
  syncTestInputs();
  scheduleValidation();
}
