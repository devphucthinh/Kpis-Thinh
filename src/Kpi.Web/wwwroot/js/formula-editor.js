const fallbackCatalog = {
  formulaLanguageVersion: 1,
  astSchemaVersion: 1,
  operators: [
    { name: "+", kind: "operator", signature: "left + right", description: "Cộng hai Decimal.", example: "revenue + bonus", insertText: " + " },
    { name: "-", kind: "operator", signature: "left - right", description: "Trừ hai Decimal.", example: "revenue - discount", insertText: " - " },
    { name: "*", kind: "operator", signature: "left * right", description: "Nhân hai Decimal.", example: "units * price", insertText: " * " },
    { name: "/", kind: "operator", signature: "left / right", description: "Chia hai Decimal.", example: "revenue / target", insertText: " / " },
    { name: "%", kind: "operator", signature: "value%", description: "Phần trăm postfix.", example: "discount%", insertText: "%" },
    { name: "MOD", kind: "operator", signature: "left MOD right", description: "Phần dư Decimal.", example: "worked MOD 7", insertText: " MOD " },
    { name: "AND", kind: "operator", signature: "left AND right", description: "AND giữa hai Boolean.", example: "active AND approved", insertText: " AND " },
    { name: "OR", kind: "operator", signature: "left OR right", description: "OR giữa hai Boolean.", example: "manual OR automatic", insertText: " OR " },
    { name: "NOT", kind: "operator", signature: "NOT value", description: "Đảo Boolean.", example: "NOT archived", insertText: "NOT " }
  ],
  functions: [
    { name: "IF", kind: "function", signature: "IF(condition, whenTrue, whenFalse)", description: "Chọn một trong hai nhánh.", example: "IF(active, revenue, 0)", insertText: "IF()" },
    { name: "ROUND", kind: "function", signature: "ROUND(value, decimals)", description: "Làm tròn Decimal.", example: "ROUND(revenue / target * 100, 2)", insertText: "ROUND()" },
    { name: "ABS", kind: "function", signature: "ABS(value)", description: "Giá trị tuyệt đối.", example: "ABS(actual - target)", insertText: "ABS()" },
    { name: "MOD", kind: "function", signature: "MOD(left, right)", description: "Dạng hàm của phần dư.", example: "MOD(worked, 7)", insertText: "MOD()" }
  ]
};

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

const functionSuggestions = ["IF", "ROUND", "ABS", "MOD", "AND", "OR", "NOT", "%"];

function normalizeCatalog(value) {
  return value && Array.isArray(value.operators) && Array.isArray(value.functions) ? value : fallbackCatalog;
}

async function loadCatalog(url) {
  if (!url) return fallbackCatalog;
  try {
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) return fallbackCatalog;
    const data = await response.json();
    return normalizeCatalog(data.supportedOperations || data);
  } catch {
    return fallbackCatalog;
  }
}

function tokenRange(textarea) {
  const cursor = textarea.selectionStart;
  const before = textarea.value.slice(0, cursor);
  const match = before.match(/[A-Za-z_][A-Za-z0-9_]*$/);
  return match ? { start: cursor - match[0].length, end: cursor, query: match[0] } : { start: cursor, end: cursor, query: "" };
}

function variableSuggestions(rows) {
  return rows.filter(row => row.code).map(row => ({
    name: row.code,
    kind: "variable",
    signature: row.code,
    description: row.description || `${row.type || "Decimal"} formula variable`,
    example: row.code,
    insertText: row.code
  }));
}

export function attachFormulaEditor(source, variableInput, diagnostics, astPreview, testInputs, testButton, testResult, variableRows = null, variablesJson = null, suggestionsPanel = null, syntaxHelper = null) {
  if (!source || !variableInput || !diagnostics || !astPreview || !testInputs || !testButton || !testResult) return;
  const panel = suggestionsPanel || document.getElementById("formula-suggestions-panel");
  const helper = syntaxHelper || document.getElementById("formula-syntax-helper");
  let timer;
  let rows = parseVariableRows(variableInput, variablesJson);
  let catalog = fallbackCatalog;
  let activeSuggestions = [];
  let activeIndex = -1;

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
    rows.forEach(row => {
      const card = document.createElement("div");
      card.className = "panel formula-variable-row";
      card.dataset.variableRow = "true";
      const top = document.createElement("div");
      top.className = "formula-variable-grid";
      top.innerHTML = `
        <label>Code<input data-variable-field="code" autocomplete="off"></label>
        <label>Tên hiển thị<input data-variable-field="displayName"></label>
        <label>Kiểu<select data-variable-field="type"><option value="Decimal">Decimal</option><option value="Boolean">Boolean</option></select></label>`;
      const bottom = document.createElement("div");
      bottom.className = "formula-variable-grid formula-variable-grid-secondary";
      bottom.innerHTML = `
        <label><input data-variable-field="required" type="checkbox"> Bắt buộc</label>
        <label>Default<input data-variable-field="defaultValue"></label>
        <label>Mô tả<input data-variable-field="description"></label>`;
      card.append(top, bottom);
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
    syncTestInputs();
    scheduleValidation();
    renderSuggestions();
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

  const renderSyntaxHelper = item => {
    if (!helper) return;
    helper.replaceChildren();
    const signature = document.createElement("strong");
    signature.textContent = item ? item.signature : "Cú pháp hỗ trợ";
    const description = document.createElement("span");
    description.textContent = item ? ` — ${item.description}` : " — Chọn một phép toán để xem tham số.";
    const example = document.createElement("code");
    example.textContent = item ? `Ví dụ: ${item.example}` : "";
    helper.append(signature, description, example);
  };

  const markActiveSuggestion = () => {
    if (!panel) return;
    Array.from(panel.querySelectorAll("[role='option']")).forEach((option, index) => {
      const selected = index === activeIndex;
      option.setAttribute("aria-selected", selected ? "true" : "false");
      if (selected) option.scrollIntoView({ block: "nearest" });
    });
    if (activeSuggestions[activeIndex]) renderSyntaxHelper(activeSuggestions[activeIndex]);
  };

  const hideSuggestions = () => {
    if (!panel) return;
    panel.hidden = true;
    panel.replaceChildren();
    activeSuggestions = [];
    activeIndex = -1;
  };

  const selectSuggestion = item => {
    const range = tokenRange(source);
    const insert = item.insertText || item.name;
    source.setRangeText(insert, range.start, range.end, "end");
    const caret = item.kind === "function" ? range.start + insert.length - 1 : range.start + insert.length;
    source.setSelectionRange(caret, caret);
    renderSyntaxHelper(item);
    hideSuggestions();
    source.dispatchEvent(new Event("input", { bubbles: true }));
  };

  function renderSuggestions() {
    if (!panel) return;
    const range = tokenRange(source);
    if (!range.query) { hideSuggestions(); return; }
    const query = range.query.toUpperCase();
    const operations = [...(catalog.operators || []), ...(catalog.functions || []), ...variableSuggestions(rows)];
    activeSuggestions = operations.filter(item => item.name.toUpperCase().startsWith(query)).slice(0, 10);
    if (activeSuggestions.length === 0) { hideSuggestions(); return; }
    panel.replaceChildren();
    activeIndex = 0;
    activeSuggestions.forEach((item, index) => {
      const option = document.createElement("button");
      option.type = "button";
      option.role = "option";
      option.setAttribute("aria-selected", index === 0 ? "true" : "false");
      option.className = "formula-suggestion-option";
      option.textContent = `${item.name} · ${item.signature}`;
      option.addEventListener("mouseenter", () => { activeIndex = index; markActiveSuggestion(); });
      option.addEventListener("click", () => selectSuggestion(item));
      panel.append(option);
    });
    panel.hidden = false;
    renderSyntaxHelper(activeSuggestions[0]);
  }

  const handleSuggestionKeydown = event => {
    if (!panel || panel.hidden || activeSuggestions.length === 0) return;
    if (event.key === "ArrowDown") { event.preventDefault(); activeIndex = (activeIndex + 1) % activeSuggestions.length; markActiveSuggestion(); }
    else if (event.key === "ArrowUp") { event.preventDefault(); activeIndex = (activeIndex - 1 + activeSuggestions.length) % activeSuggestions.length; markActiveSuggestion(); }
    else if (event.key === "Enter") { event.preventDefault(); selectSuggestion(activeSuggestions[activeIndex]); }
    else if (event.key === "Escape") { event.preventDefault(); hideSuggestions(); }
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
      if (data.supportedOperations) catalog = normalizeCatalog(data.supportedOperations);
      renderSuggestions();
    }, 250);
  };

  source.setAttribute("autocomplete", "off");
  source.dataset.autocomplete = functionSuggestions.join(",");
  source.addEventListener("input", () => { scheduleValidation(); renderSuggestions(); });
  source.addEventListener("focus", renderSuggestions);
  source.addEventListener("keydown", handleSuggestionKeydown);
  variableInput.addEventListener("input", () => { rows = parseVariableCodes(variableInput.value); renderRows(); syncTestInputs(); scheduleValidation(); renderSuggestions(); });
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
  renderSyntaxHelper(null);
  loadCatalog(source.dataset.formulaCapabilitiesUrl).then(loaded => { catalog = loaded; renderSuggestions(); });
  scheduleValidation();
}
