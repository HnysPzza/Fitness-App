# Task.md — Debug Mode: Record Screen Issues

## Overview
Two confirmed issues on the **Record** screen of the Android application:

1. **UI Bug** — Misaligned/duplicate Record button (visual glitch)
2. **Crash** — Page crashes when run independently on device (likely memory/rendering related)

---

## Issue 1: Mismatched Record Button (UI Bug)

### Observed Behavior
From the screenshot, there are **two orange circular elements** visible:
- A **blurred/glowing orange orb** (larger, unfocused) sitting above the actual button
- A **sharp orange play button** (the real CTA button) below it

This suggests a **ghost/shadow view** is being rendered behind the actual button — likely a duplicate layer or an incorrectly positioned View with the same background color.

### Likely Root Causes
- A `View` or `ImageView` with an orange background is positioned absolutely and overlapping the button
- An **elevation shadow** or **ripple drawable** is leaking outside its bounds
- A **Lottie animation** or **pulse animation** for the record button is not being clipped correctly
- A `CoordinatorLayout` or `ConstraintLayout` child with `match_parent` height is causing a stacked duplicate

### Debugging Steps
- [ ] Inspect the layout using **Android Studio Layout Inspector** on the Record fragment/activity
- [ ] Search for any `View` with `@color/orange` or similar background that is **not** the primary button
- [ ] Check if there is a **pulse/ripple animation view** (e.g., `RecordPulseView`, `AnimationView`) that should be `GONE` or clipped but is showing
- [ ] Verify `z-index` / `elevation` ordering of all children in the Record screen layout XML
- [ ] Check if the issue is reproducible on all devices or specific screen densities/sizes

### Files to Investigate
- `fragment_record.xml` (or equivalent layout file)
- `RecordFragment.kt` / `RecordActivity.kt`
- Any custom view class related to the record button (e.g., `RecordButton.kt`, `PulseAnimationView.kt`)
- Button drawable/style definitions in `res/drawable/` or `res/values/styles.xml`

---

## Issue 3: Floating Container / Bottom Sheet Styling Bug

### Observed Behavior
From the second screenshot, the bottom panel (containing the sport selector, GPS status, and record button) appears as a **floating card** that does not extend to the screen edges — it has visible gaps/spacing on the left and right sides, giving it an unintended "floating" look rather than a full-width anchored bottom sheet.

Additionally:
- The container appears to have **rounded corners on all four sides**, which is inconsistent with a standard bottom sheet (should only round the **top two corners**)
- There is visible **margin/padding on the horizontal sides** that separates the card from the screen edges
- The overall effect suggests the container has been given explicit `margin`, `width` less than `match_parent`, or a `wrap_content` constraint that is shrinking it

### Likely Root Causes
- The bottom sheet layout has `android:layout_marginStart` / `android:layout_marginEnd` set unintentionally
- The root `View` or `CardView` wrapping the panel has a fixed `dp` width instead of `match_parent`
- A `BottomSheetDialogFragment` is being used with a theme that sets `android:windowBackground` padding or `dialog_sheet_shape` with rounded corners on all sides
- The `BottomSheetBehavior` `peekHeight` container is wrapped inside a `CardView` with elevation + margin instead of being the sheet itself
- In `ConstraintLayout`, the sheet might be constrained with `app:layout_constraintWidth_percent` less than 1.0

### Debugging Steps
- [ ] Open `fragment_record.xml` (or the bottom sheet layout file) and inspect the **root ViewGroup** — check for any `layout_margin`, `padding`, or fixed `layout_width`
- [ ] If using `BottomSheetDialogFragment`, check the theme in `res/values/themes.xml` for `bottomSheetStyle` — ensure `android:layout_width` is `match_parent`
- [ ] If using a `CardView`, replace with a plain `LinearLayout` or `ConstraintLayout` with a custom background drawable that only rounds the top corners:
  ```xml
  <!-- res/drawable/bg_bottom_sheet.xml -->
  <shape xmlns:android="http://schemas.android.com/apk/res/android">
      <corners android:topLeftRadius="16dp" android:topRightRadius="16dp" />
      <solid android:color="#FF1F2A3C" />
  </shape>
  ```
- [ ] Ensure the bottom sheet container uses:
  ```xml
  android:layout_width="match_parent"
  android:layout_marginStart="0dp"
  android:layout_marginEnd="0dp"
  ```
- [ ] If `BottomSheetBehavior` is used, confirm the behavior is attached to a view that spans the full width of the `CoordinatorLayout`

### Files to Investigate
- `fragment_record.xml` — root container of the bottom panel
- `res/values/themes.xml` / `res/values/styles.xml` — `bottomSheetStyle`, `shapeAppearanceLargeComponent`
- `RecordFragment.kt` — check if margins are being set programmatically in `onViewCreated`

---

## Issue 2: Page Crash When Run Independently (Stability Bug)

### Observed Behavior
The Record screen **crashes when launched standalone** on a physical Android device. This does not appear to be a logic crash but is likely related to **heavy UI rendering or missing initialization context**.

### Likely Root Causes

#### A. Heavy UI / Memory Pressure
- The page renders a **live map background** (visible in screenshot) — this is GPU/memory intensive
- If the map SDK (e.g., Mapbox, Google Maps, OSMDroid) is not properly initialized before the fragment loads, it will crash
- Large bitmaps or animation assets loaded on the main thread

#### B. Missing Dependencies When Running Independently
- The Record screen may rely on a **singleton or application-level object** (e.g., GPS manager, session manager) that is only initialized when the app starts from the Home screen
- If launched via deep link or directly (e.g., from Android Studio's "Run specific activity"), those singletons are `null` → NullPointerException

#### C. GPS / Location Service Init
- `GPS Ready` state shown in UI implies location permissions and services are accessed on launch
- If `LocationManager` or `FusedLocationProviderClient` is accessed before permission check completes, this can crash

### Debugging Steps
- [ ] Run the Record screen independently and capture the **full crash logcat** (`adb logcat -s AndroidRuntime`)
- [ ] Look for `NullPointerException`, `UninitializedPropertyAccessException`, or `IllegalStateException` in the stack trace
- [ ] Wrap map initialization in a null-safe check and ensure the Map SDK lifecycle is tied to the fragment lifecycle
- [ ] Audit all `lateinit var` properties in `RecordFragment`/`RecordViewModel` — add `isInitialized` guards or use lazy initialization
- [ ] Move heavy initialization (map, GPS, animations) off the main thread using `lifecycleScope.launch(Dispatchers.IO)`
- [ ] Check `Application.onCreate()` — confirm all required singletons are initialized there and not lazily elsewhere
- [ ] Add try-catch around the map rendering block as a temporary diagnostic measure

### Files to Investigate
- `RecordFragment.kt` / `RecordActivity.kt` — `onViewCreated`, `onResume`, `onStart`
- `RecordViewModel.kt` — check for unguarded lateinit access
- `AndroidManifest.xml` — confirm the activity has `android:exported="true"` and correct intent filters if testing via direct launch
- Application class (e.g., `App.kt`) — verify initialization order

---

## Priority

| Issue | Severity | Priority |
|---|---|---|
| Crash on independent launch | High — blocks QA and development testing | P0 |
| Mismatched duplicate button | Medium — visual defect, affects UX | P1 |
| Floating container not full-width | Medium — visual defect, inconsistent UI | P1 |

---

## Acceptance Criteria
- [ ] Record screen launches without crash when started independently on a physical Android device
- [ ] Only **one** orange record button is visible — no ghost/blur duplicate
- [ ] GPS initialization and map rendering complete without ANR or memory crash
- [ ] Layout Inspector shows no unexpected overlapping Views on the Record screen
- [ ] Bottom sheet container spans **full screen width** with no side margins
- [ ] Bottom sheet has rounded **top corners only**, flush against screen edges at the bottom