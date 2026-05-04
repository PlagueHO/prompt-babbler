---
applyTo: '.copilot-tracking/changes/2026-05-04/babble-tags-colored-display-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Babble Tags — Colored Display and Consistent Editing UX

## Overview

Add deterministic hash-based colored badges for babble tags across all display and editing components, using Tailwind CSS classes on the existing Shadcn/UI Badge component.

## Objectives

### User Requirements

* Display tags as colored pill-shaped bubbles/badges alongside babbles in all views — Source: Task request
* Use deterministic color assignment so the same tag always renders the same color — Source: Task request
* Tags must be editable inline with the babble using a standard Shadcn/UI component — Source: Task request (already satisfied by existing TagInput in BabbleEditor)

### Derived Objectives

* Add dark mode support for tag colors — Derived from: Project uses dark mode (Tailwind `dark:` classes), tags should be legible in both modes
* Apply consistent coloring in TagInput during editing — Derived from: UX consistency requires same tag = same color in read and edit modes
* Add unit tests for the color utility — Derived from: Project testing conventions require tests for new utility modules

## Context Summary

### Project Files

* prompt-babbler-app/src/components/ui/tag-list.tsx - Read-only tag display (20 lines); used by BabbleCard, BabbleListItem, BabbleBubbles
* prompt-babbler-app/src/components/ui/tag-input.tsx - Editable tag input (138 lines); used by BabbleEditor, TemplateEditor
* prompt-babbler-app/src/components/ui/badge.tsx - Shadcn/UI Badge with CVA variants, accepts className
* prompt-babbler-app/src/lib/utils.ts - Existing lib utilities (cn helper)
* prompt-babbler-app/tests/components/ui/TagList.test.tsx - Existing TagList tests
* prompt-babbler-app/tests/components/ui/TagInput.test.tsx - Existing TagInput tests

### References

* .copilot-tracking/research/2026-05-04/babble-tags-feature-research.md - Full research document with code examples and architecture analysis

### Standards References

* .github/copilot-instructions.md — React patterns: named exports, `@/` alias, components in `src/components/`, lib/utils in `src/lib/`
* AGENTS.md — Frontend naming: camelCase functions, kebab-case files; Testing: Vitest + Testing Library

## Implementation Checklist

### [x] Implementation Phase 1: Create Tag Color Utility

<!-- parallelizable: true -->

* [x] Step 1.1: Create `src/lib/tag-colors.ts` with color palette and hash function
  * Details: .copilot-tracking/details/2026-05-04/babble-tags-colored-display-details.md (Lines 9-44)
* [x] Step 1.2: Create unit tests for tag color utility
  * Details: .copilot-tracking/details/2026-05-04/babble-tags-colored-display-details.md (Lines 46-78)
* [x] Step 1.3: Validate phase changes
  * Run `pnpm lint` and `pnpm test` for tag-colors tests

### [x] Implementation Phase 2: Update Display and Editing Components

<!-- parallelizable: true -->

* [x] Step 2.1: Update TagList to apply deterministic colors
  * Details: .copilot-tracking/details/2026-05-04/babble-tags-colored-display-details.md (Lines 80-110)
* [x] Step 2.2: Update TagInput to apply deterministic colors
  * Details: .copilot-tracking/details/2026-05-04/babble-tags-colored-display-details.md (Lines 112-138)
* [x] Step 2.3: Update TagList and TagInput tests to verify colored output
  * Details: .copilot-tracking/details/2026-05-04/babble-tags-colored-display-details.md (Lines 140-175)
* [x] Step 2.4: Validate phase changes
  * Run `pnpm lint` and `pnpm test`

### [x] Implementation Phase 3: Final Validation

<!-- parallelizable: false -->

* [x] Step 3.1: Run full project validation
  * Execute `pnpm lint` (ESLint)
  * Execute `pnpm run build` (TypeScript + Vite)
  * Execute `pnpm test` (Vitest full suite)
* [x] Step 3.2: Fix minor validation issues
  * Iterate on lint errors and build warnings
  * Apply fixes directly when corrections are straightforward
* [x] Step 3.3: Report blocking issues
  * Document issues requiring additional research
  * Provide user with next steps and recommended planning

## Planning Log

See .copilot-tracking/plans/logs/2026-05-04/babble-tags-colored-display-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* Tailwind CSS (already installed — all color utilities available)
* Shadcn/UI Badge component (already installed)
* Vitest + Testing Library (already installed)

## Success Criteria

* Tags display as colored pill badges in BabbleCard, BabbleListItem, BabbleBubbles — Traces to: User requirement (colored display)
* Same tag text always renders the same color deterministically — Traces to: User requirement (deterministic coloring)
* Tags show matching colors in TagInput during editing — Traces to: Derived objective (UX consistency)
* Colors are legible in both light and dark mode — Traces to: Derived objective (dark mode support)
* Unit tests pass for getTagColor utility with 100% coverage — Traces to: Derived objective (testing)
* No lint errors, build passes, all existing tests still pass — Traces to: Project CI requirements
