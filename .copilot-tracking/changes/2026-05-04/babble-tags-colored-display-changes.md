<!-- markdownlint-disable-file -->
# Release Changes: Babble Tags — Colored Display and Consistent Editing UX

**Related Plan**: babble-tags-colored-display-plan.instructions.md
**Implementation Date**: 2026-05-04

## Summary

Add deterministic hash-based colored badges for babble tags across all display and editing components, using Tailwind CSS classes on the existing Shadcn/UI Badge component.

## Changes

### Added

* `prompt-babbler-app/src/lib/tag-colors.ts` — New tag color utility: 17-color Tailwind palette + djb2 hash function, exports `getTagColor(tag: string): string`
* `prompt-babbler-app/tests/lib/tag-colors.test.ts` — Unit tests for `getTagColor`: determinism, case-insensitivity, format validation, edge cases (6 tests, all pass)

### Modified

* `prompt-babbler-app/src/components/ui/tag-list.tsx` — Added `getTagColor` import; updated Badge `className` to apply deterministic color with `border-transparent`
* `prompt-babbler-app/src/components/ui/tag-input.tsx` — Added `getTagColor` import; updated tag-pill Badge `className` to apply deterministic color with `border-transparent`
* `prompt-babbler-app/tests/components/ui/TagList.test.tsx` — Added 2 new tests: color class presence and determinism assertions
* `prompt-babbler-app/tests/components/ui/TagInput.test.tsx` — Added 1 new test: color class presence on rendered tag badges

### Removed

<!-- None -->

## Additional or Deviating Changes

<!-- To be populated if deviations occur -->

## Release Summary

**Total files affected**: 6 (2 added, 4 modified, 0 removed)

### Files Created

* `prompt-babbler-app/src/lib/tag-colors.ts` — Tag color utility: 17-color Tailwind palette (red through rose, full spectrum) with djb2 string hash. Exports `getTagColor(tag: string): string` returning deterministic light+dark Tailwind class strings.
* `prompt-babbler-app/tests/lib/tag-colors.test.ts` — 6 unit tests for `getTagColor` covering determinism, case-insensitivity, format validation, empty string, and special characters.

### Files Modified

* `prompt-babbler-app/src/components/ui/tag-list.tsx` — Imports `getTagColor`; Badge `className` updated to apply deterministic color + `border-transparent`. Affects BabbleCard, BabbleListItem, BabbleBubbles automatically.
* `prompt-babbler-app/src/components/ui/tag-input.tsx` — Imports `getTagColor`; tag-pill Badge `className` updated with deterministic color + `border-transparent`. Affects BabbleEditor and TemplateEditor editing views.
* `prompt-babbler-app/tests/components/ui/TagList.test.tsx` — 2 new tests: color class presence on rendered badges; color determinism across rerenders.
* `prompt-babbler-app/tests/components/ui/TagInput.test.tsx` — 1 new test: color class presence on rendered tag badges in edit mode.

### Dependency and Infrastructure Notes

* No new dependencies added. Tailwind CSS, Shadcn/UI Badge, and Vitest were all already installed.
* Tailwind static string scanning confirmed to pick up the `TAG_COLORS` array — no safelist configuration needed (WI-05 resolved as non-issue).

### Validation

* `pnpm lint` — passed, 0 errors
* `pnpm run build` — passed (pre-existing unrelated warnings only)
* `pnpm test` — 135/135 tests passed across 27 test files
