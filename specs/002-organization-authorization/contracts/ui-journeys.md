# UI Journey Contract

The reference UI remains Vietnamese-first ASP.NET Core MVC/Razor. It uses the
JSON/Application contracts and owns presentation only. Every POST is authorized
again by the backend even when an action is hidden or disabled.

## Navigation

```text
Quản trị
|-- Sơ đồ tổ chức
|   |-- Cấu trúc đơn vị
|   |-- Vị trí và quan hệ báo cáo
|   |-- Nhân viên và phân công vị trí
|   |-- Baseline cơ cấu
|   `-- Không gian KPI theo cơ cấu
|-- Bảo mật KPI
|   |-- Danh mục tác vụ
|   |-- Vai trò tùy chỉnh
|   |-- Gán vai trò và phạm vi
|   `-- Chính sách phê duyệt đặc quyền
`-- Phê duyệt và kiểm toán
    |-- Hàng đợi phê duyệt
    |-- Ủy quyền
    `-- Dòng thời gian
```

Navigation is filtered for usability by the reduced `/me/actions` projection;
direct URLs and POSTs still pass the authoritative Application decision.

## Journey 1: Organization to approved baseline

1. Open **Sơ đồ tổ chức** and view the current workspace revision.
2. Add/edit unit nodes, Positions, Employees, Position Assignments, and direct
   reporting relationships using server forms.
3. Save with the current concurrency token. A stale token keeps the user's
   submitted values and shows the current revision instead of overwriting it.
4. Select **Kiểm tra cấu trúc**. Validation groups errors by unit/Position/
   Employee and highlights the full cycle or conflicting interval path.
5. Select **Gửi duyệt baseline**, enter an effective time and mandatory reason,
   and review a diff from the previous approved baseline.
6. A different eligible actor opens the approval queue. The page shows the
   frozen revision hash, structure diff, selector evidence, and effective range.
7. Approval creates the scheduled/effective baseline. The detail page becomes
   read-only and links the approval timeline and prior baseline.
8. A successor approval preview shows the current chain tail. Approval
   atomically closes it at the successor start and opens the successor; a gap,
   overlap, out-of-order insertion, or stale tail is a blocking diagnostic.

Required responsive behavior: the tree becomes an indented list at 390 px;
every edit/validation/approval action remains reachable by keyboard; errors do
not depend on color alone.

## Journey 2: Custom role like a business-task admin center

1. Open **Vai trò tùy chỉnh** and create a role name/description.
2. Capabilities are grouped by business area. Each row shows task name,
   description, risk badge, applicable scopes, and a details disclosure.
3. Selecting conflicting maker/approver capabilities displays a warning panel
   listing the combination and runtime separation-of-duty rule.
4. The administrator acknowledges warnings and saves an immutable role version.
5. Editing a used bundle opens **Tạo phiên bản mới** with a before/after diff;
   existing Role Assignments remain linked to the old version.

The UI never allows creation of a capability identifier and never suggests that
role creation grants the role to its creator.

## Journey 3: Scoped Role Assignment and independent approval

1. Choose Employee, exact role version, effective interval, and one KPI Data
   Scope. UnitSubtree selection is anchored to an approved baseline.
2. A privilege preview explains whether approval is required and whether risk,
   scope, or an always-approve capability caused it.
3. Submit with a reason. Self-approval by requester or beneficiary returns an
   in-page separation-of-duty explanation and an audit correlation ID.
4. An independent approver sees role version, atomic tasks, scope, beneficiary,
   effective dates, policy version, requester, and reason.
5. Approval schedules/activates the assignment; rejection keeps the immutable
   decision and offers a new proposal revision.
6. A verification panel shows allowed in-scope and denied out-of-scope example
   resources using backend decisions, not client simulation.

## Journey 4: Route resolution and delegation

1. A route owner creates a draft route version with ordered stages, a primary
   typed selector, explicit fallbacks, required capability, and required scope
   relation. `Organization Unit Head` requires Unit plus explicit Employee;
   `Named Group` selects an internal effective-dated Approval Group.
2. Editing an active/used route creates a new version with a before/after diff
   and opaque head token. A stale editor receives a conflict and current head;
   existing route snapshots remain on the original version.
3. Validation reports configuration errors without activating the route. The
   owner submits a frozen version and a different actor with applicable
   `approval.route.approve` capability reviews its diff and reason. The maker or
   editor cannot approve or activate the same version.
4. An eligible non-maker activates only an approved version. If another route
   is active for the artifact type, the page shows both identities and performs
   one atomic switch; attempting to retire the active route without an approved
   replacement is blocked with **Cần route thay thế đã duyệt**.
5. Submitting an artifact shows the applicable baseline and a preview of
   resolved candidates. No eligible primary/fallback blocks submission with the
   failed selector path.
6. A Direct Manager stage shows whether it used artifact Position context or
   the allowed primary-Position fallback. A Named Group stage shows the frozen
   effective member/candidate evidence. Later manager or group changes do not
   change the snapshot.
7. The frozen timeline shows selector, candidate evidence, resolved actor,
   Position/group evidence, fallback, scope, route review, activation, and
   baseline revision.
8. An original approver creates a time/scope/responsibility-limited delegation.
9. A delegate decision displays **Thực hiện thay cho** and preserves both
   identities. Expired/out-of-scope delegation is rejected without skipping the
   stage.

## Journey 5: Mid-period baseline impact

1. A future/mid-period structure revision diff identifies moved units, changed
   Positions, changed Employees/assignments, and affected downstream references.
2. The approval page warns when the effective instant is inside an open KPI
   period and marks **Yêu cầu tái phân rã**.
3. The impact page displays prior/new baseline, effective boundary, derived
   **Detected** state, and a proportional weight preview. It has no manual
   acknowledge/resolve control.
4. For old weights 50/20/30 and a fixed new 20, the preview shows
   40/16/24/20, residual allocation details, unchanged relative order, and an
   exact 100% proof total.
5. After later Planning independently approves the exact KPI Plan Amendment and
   registers the immutable resolution through the Application contract, the
   read projection shows **Resolved** with amendment revision, approval actor/
   time, and correlation evidence. A direct status toggle is never presented.
6. The UI does not claim that KPI results were recalculated. It links impact and
   resolution facts that later Planning/Evaluation journeys consume.
7. The baseline timeline proves predecessor applicability ends exactly when the
   successor begins; after the first baseline start there is no ordinary empty
   baseline state.

## Timeline visibility

The timeline query returns only events the actor may view within Organization
tree scope. Each visible entry shows:

- occurred time and stable action label;
- actor and represented authority/delegate when applicable;
- artifact and revision;
- selector/fallback/delegation evidence;
- decision, human reason, and stable system reason;
- scope summary and authorization impact;
- correlation ID for support.

Hidden entries are omitted rather than replaced by revealing placeholders. The
immutable Audit Record remains intact regardless of current visibility.

## Journey 6: Organization KPI Workspace foundation

1. Open **Không gian KPI theo cơ cấu**. The server resolves the applicable
   approved baseline and exact Baseline Applicability Segment for the URL's
   `effectiveAt`.
2. The lazy tree returns only Organization Units and Positions allowed by
   `organization.structure.view` plus KPI Data Scope. Organization Unit nodes
   expand/collapse; they never select or aggregate KPIs.
3. Select a Position. The URL records Position, baseline, effective time,
   branch, and search state. Refresh, back, forward, and a copied URL restore
   the same authorized Position.
4. A direct URL to an out-of-scope Position returns a safe forbidden/not-found
   experience without selecting another Position or revealing hidden ancestry.
5. Until later Planning/Cascade/Actual/Evaluation providers exist, the detail
   region explains that the KPI neighborhood is unavailable. It does not render
   mock Target, Actual, Variance, score, weights, or KPI Effective Segment.
6. At 390 pixels the tree opens in **Chọn vị trí**; keyboard arrow/Enter
   behavior, focus restoration, and all context evidence remain available.

The later workspace journeys consume
`contracts/organization-kpi-workspace.md` to add the exactly-one-edge KPI table
and official results without changing these navigation or authorization rules.

## Visual and accessibility states

- Lifecycle, data, and decision status use text plus badge/icon; never color
  alone.
- Warnings remain warnings and are visually distinct from blocking errors.
- Validation summary links focus to the exact offending form/tree item.
- Read-only approved snapshots are visibly locked and show revision/effective
  evidence.
- All dialogs use native/server form flows or accessible existing patterns; no
  new business JavaScript is required.
- Desktop and 390-pixel views preserve submit/review reason fields, warning
  acknowledgements, timeline evidence, and primary actions.

## Playwright acceptance journey

Using seeded Development personas with distinct Employee/account identities:

1. Organization Admin builds and validates structure.
2. Submitter submits baseline; attempted self-approval is denied.
3. Independent approver approves; application restarts on PostgreSQL; baseline
   remains effective.
4. Security Admin creates a risky custom role and acknowledges warnings.
5. Security Admin requests a UnitSubtree assignment; self-elevation is denied;
   independent approver activates it.
6. Beneficiary succeeds on an in-subtree operation and is denied outside it.
7. Approver delegates one stage; delegate acts within time/scope; an invalid
   delegated attempt is denied.
8. Auditor views the permitted timeline; an out-of-scope observer cannot see
   protected details.
9. A mid-period baseline change produces impact and deterministic weight
   preview evidence.
10. The authorized Organization KPI Workspace restores an in-scope Position
    URL after restart, blocks an out-of-scope Position safely, and displays the
    honest future-provider state without KPI fixtures.

The journey runs with keyboard assertions and a 390-pixel viewport and is
repeated after Web restart to prove durable PostgreSQL behavior.
