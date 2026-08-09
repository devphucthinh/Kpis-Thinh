(() => {
  const storageKey = "kpi-ui-theme";
  const root = document.documentElement;
  const toggle = document.querySelector("[data-theme-toggle]");
  const label = document.querySelector("[data-theme-label]");
  const systemTheme = () => window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  const readTheme = () => {
    try { return localStorage.getItem(storageKey) || systemTheme(); } catch { return systemTheme(); }
  };
  const setTheme = theme => {
    root.dataset.theme = theme;
    if (label) label.textContent = theme === "dark" ? "Light mode" : "Dark mode";
    if (toggle) toggle.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
  };
  setTheme(readTheme());
  toggle?.addEventListener("click", () => {
    const next = root.dataset.theme === "dark" ? "light" : "dark";
    setTheme(next);
    try { localStorage.setItem(storageKey, next); } catch { /* local preference is optional */ }
  });
})();
