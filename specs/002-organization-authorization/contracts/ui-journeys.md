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
|   `-- Baseline cơ cấu
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
   selector, explicit fallbacks, required capability, and required scope
   relation; validation/activation is a governed action.
2. Editing an active/used route creates a new version with a before/after diff
   and opaque head token. A stale editor receives a conflict and current head;
   existing route snapshots remain on the original version.
3. Submitting an artifact shows the applicable baseline and a preview of
   resolved candidates. No eligible primary/fallback blocks submission with the
   failed selector path.
4. The frozen timeline shows selector, candidate evidence, resolved actor,
   fallback, scope, and baseline revision.
5. An original approver creates a time/scope/responsibility-limited delegation.
6. A delegate decision displays **Thực hiện thay cho** and preserves both
   identities. Expired/out-of-scope delegation is rejected without skipping the
   stage.

## Journey 5: Mid-period baseline impact

1. A future/mid-period structure revision diff identifies moved units, changed
   Positions, changed Employees/assignments, and affected downstream references.
2. The approval page warns when the effective instant is inside an open KPI
   period and marks **Yêu cầu tái phân rã**.
3. The impact page displays prior/new baseline, effective boundary, unresolved
   downstream amendment state, and a proportional weight preview.
4. For old weights 50/20/30 and a fixed new 20, the preview shows
   40/16/24/20, residual allocation details, unchanged relative order, and an
   exact 100% proof total.
5. The UI does not claim that KPI results were recalculated. It links the impact
   fact that later Planning/Evaluation journeys must resolve.
6. The baseline timeline proves predecessor applicability ends exactly when the
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

The journey runs with keyboard assertions and a 390-pixel viewport and is
repeated after Web restart to prove durable PostgreSQL behavior.
