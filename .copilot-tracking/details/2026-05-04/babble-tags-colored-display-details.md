<!-- markdownlint-disable-file -->
# Implementation Details: Babble Tags — Colored Display and Consistent Editing UX

## Context Reference

Sources: .copilot-tracking/research/2026-05-04/babble-tags-feature-research.md

## Implementation Phase 1: Create Tag Color Utility

<!-- parallelizable: true -->

### Step 1.1: Create `src/lib/tag-colors.ts` with color palette and hash function

Create a new utility file that exports a deterministic tag-to-color mapping function. The function uses a djb2-style string hash to map any tag string (case-insensitive) to one of 17 Tailwind color class combinations supporting both light and dark mode.

Files:
* prompt-babbler-app/src/lib/tag-colors.ts - NEW: tag color utility

Implementation:

```typescript
const TAG_COLORS = [
  'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200',
  'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200',
  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  'bg-lime-100 text-lime-800 dark:bg-lime-900 dark:text-lime-200',
  'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
  'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200',
  'bg-teal-100 text-teal-800 dark:bg-teal-900 dark:text-teal-200',
  'bg-cyan-100 text-cyan-800 dark:bg-cyan-900 dark:text-cyan-200',
  'bg-sky-100 text-sky-800 dark:bg-sky-900 dark:text-sky-200',
  'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
  'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200',
  'bg-violet-100 text-violet-800 dark:bg-violet-900 dark:text-violet-200',
  'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
  'bg-fuchsia-100 text-fuchsia-800 dark:bg-fuchsia-900 dark:text-fuchsia-200',
  'bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200',
  'bg-rose-100 text-rose-800 dark:bg-rose-900 dark:text-rose-200',
] as const;

function hashString(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash |= 0;
  }
  return Math.abs(hash);
}

export function getTagColor(tag: string): string {
  const index = hashString(tag.toLowerCase()) % TAG_COLORS.length;
  return TAG_COLORS[index];
}
```

Success criteria:
* File exports `getTagColor` function
* Same input always returns same output (deterministic)
* Case-insensitive ("Bug" and "bug" produce the same color)
* Returns valid Tailwind class strings with light + dark mode variants

Context references:
* .copilot-tracking/research/2026-05-04/babble-tags-feature-research.md (Lines 135-168) - Complete code example

Dependencies:
* None (pure utility, no external imports)

### Step 1.2: Create unit tests for tag color utility

Create a test file for the `getTagColor` utility covering determinism, case-insensitivity, and output format.

Files:
* prompt-babbler-app/tests/lib/tag-colors.test.ts - NEW: unit tests

Implementation:

```typescript
import { describe, it, expect } from 'vitest';
import { getTagColor } from '@/lib/tag-colors';

describe('getTagColor', () => {
  it('returns a string containing Tailwind color classes', () => {
    const result = getTagColor('test');
    expect(result).toMatch(/^bg-\w+-\d+ text-\w+-\d+ dark:bg-\w+-\d+ dark:text-\w+-\d+$/);
  });

  it('returns the same color for the same tag', () => {
    const color1 = getTagColor('feature');
    const color2 = getTagColor('feature');
    expect(color1).toBe(color2);
  });

  it('is case-insensitive', () => {
    expect(getTagColor('Bug')).toBe(getTagColor('bug'));
    expect(getTagColor('BUG')).toBe(getTagColor('bug'));
  });

  it('returns different colors for different tags', () => {
    const colors = new Set(['alpha', 'beta', 'gamma', 'delta', 'epsilon'].map(getTagColor));
    expect(colors.size).toBeGreaterThan(1);
  });

  it('handles empty string without throwing', () => {
    expect(() => getTagColor('')).not.toThrow();
    expect(getTagColor('')).toBeTruthy();
  });

  it('handles special characters', () => {
    expect(() => getTagColor('tag-with-dashes')).not.toThrow();
    expect(() => getTagColor('tag with spaces')).not.toThrow();
    expect(() => getTagColor('日本語')).not.toThrow();
  });
});
```

Success criteria:
* All tests pass
* Covers determinism, case-insensitivity, format validation, edge cases

Context references:
* prompt-babbler-app/tests/components/ui/TagList.test.tsx - Existing test patterns for reference
* AGENTS.md - Testing conventions: Vitest + Testing Library, `describe`/`it` with natural-language names

Dependencies:
* Step 1.1 completion (getTagColor must exist)

### Step 1.3: Validate phase changes

Run lint and build for the new utility file and tests.

Validation commands:
* `pnpm lint` - Lint new files
* `pnpm test -- tests/lib/tag-colors.test.ts` - Run only the new test file

## Implementation Phase 2: Update Display and Editing Components

<!-- parallelizable: true -->

### Step 2.1: Update TagList to apply deterministic colors

Modify `tag-list.tsx` to import `getTagColor` and apply the color classes to each Badge. Change from uniform grey outline to colored background with transparent border.

Files:
* prompt-babbler-app/src/components/ui/tag-list.tsx - MODIFIED: add color import and apply to Badge className

Current code (line 14-16):
```tsx
<Badge key={tag} variant="outline" className="text-xs">
  {tag}
</Badge>
```

Updated code:
```tsx
<Badge key={tag} variant="outline" className={`text-xs border-transparent ${getTagColor(tag)}`}>
  {tag}
</Badge>
```

Additional change: Add import at top of file:
```tsx
import { getTagColor } from '@/lib/tag-colors';
```

Success criteria:
* TagList renders colored badges
* All 3 consumer components (BabbleCard, BabbleListItem, BabbleBubbles) inherit the change automatically
* No prop changes required in consuming components

Context references:
* prompt-babbler-app/src/components/ui/tag-list.tsx (Lines 1-20) - Full current source
* .copilot-tracking/research/2026-05-04/babble-tags-feature-research.md (Lines 170-186) - Updated TagList example

Dependencies:
* Phase 1 Step 1.1 (tag-colors.ts must exist)

### Step 2.2: Update TagInput to apply deterministic colors

Modify `tag-input.tsx` to import `getTagColor` and apply color classes to each Badge in the editable tag list.

Files:
* prompt-babbler-app/src/components/ui/tag-input.tsx - MODIFIED: add color import and apply to Badge className

Current code (line 91):
```tsx
<Badge key={`${tag}-${index}`} variant="secondary" className="gap-1 pr-1">
```

Updated code:
```tsx
<Badge key={`${tag}-${index}`} variant="secondary" className={`gap-1 pr-1 border-transparent ${getTagColor(tag)}`}>
```

Additional change: Add import at top of file (after existing imports):
```tsx
import { getTagColor } from '@/lib/tag-colors';
```

Success criteria:
* Tags in TagInput show the same deterministic color as in TagList
* Remove/X button remains visible and functional
* Input behavior unchanged (keyboard nav, paste, comma-split)

Context references:
* prompt-babbler-app/src/components/ui/tag-input.tsx (Lines 1-138) - Full current source
* .copilot-tracking/research/2026-05-04/babble-tags-feature-research.md (Lines 188-197) - Updated TagInput example

Dependencies:
* Phase 1 Step 1.1 (tag-colors.ts must exist)

### Step 2.3: Update TagList and TagInput tests to verify colored output

Update existing tests to verify that rendered badges have color classes applied in both read-only and editing modes.

Files:
* prompt-babbler-app/tests/components/ui/TagList.test.tsx - MODIFIED: add assertions for color classes
* prompt-babbler-app/tests/components/ui/TagInput.test.tsx - MODIFIED: add assertions for color classes

Add test case:
```tsx
it('renders tags with deterministic color classes', () => {
  render(<TagList tags={['bug', 'feature']} />);
  const badges = screen.getAllByText(/bug|feature/);
  badges.forEach((badge) => {
    expect(badge.className).toMatch(/bg-\w+-\d+/);
    expect(badge.className).toMatch(/text-\w+-\d+/);
  });
});

it('renders the same tag with the same color class', () => {
  const { rerender } = render(<TagList tags={['bug']} />);
  const firstColor = screen.getByText('bug').className;
  rerender(<TagList tags={['other', 'bug']} />);
  const secondColor = screen.getByText('bug').className;
  expect(firstColor).toBe(secondColor);
});
```

For TagInput, add:
```tsx
it('renders tag badges with color classes', () => {
  render(<TagInput value={['bug', 'feature']} onChange={() => {}} />);
  const badges = screen.getAllByText(/bug|feature/);
  badges.forEach((badge) => {
    expect(badge.closest('[class]')?.className).toMatch(/bg-\w+-\d+/);
  });
});
```

Success criteria:
* Existing TagList and TagInput tests still pass
* New tests verify color class presence and determinism in both components

Context references:
* prompt-babbler-app/tests/components/ui/TagList.test.tsx - Existing tests

Dependencies:
* Step 2.1 completion (TagList must use getTagColor)

### Step 2.4: Validate phase changes

Run full lint and test suite for modified components.

Validation commands:
* `pnpm lint` - Lint all modified files
* `pnpm test` - Run full test suite to catch regressions

## Implementation Phase 3: Final Validation

<!-- parallelizable: false -->

### Step 3.1: Run full project validation

Execute all validation commands for the project:
* `pnpm lint` - ESLint on all source files
* `pnpm run build` - Full TypeScript + Vite production build
* `pnpm test` - Complete Vitest suite

### Step 3.2: Fix minor validation issues

Iterate on lint errors, build warnings, and test failures. Apply fixes directly when corrections are straightforward and isolated.

### Step 3.3: Report blocking issues

When validation failures require changes beyond minor fixes:
* Document the issues and affected files.
* Provide the user with next steps.
* Recommend additional research and planning rather than inline fixes.
* Avoid large-scale refactoring within this phase.

## Dependencies

* Tailwind CSS (installed, all color utilities available without config changes)
* Vitest (installed, test infrastructure in place)

## Success Criteria

* getTagColor utility is deterministic and case-insensitive
* TagList and TagInput display colored badges
* All existing tests pass with no regressions
* New tests cover utility and component coloring behavior
* Build succeeds with no TypeScript errors
* Lint passes with no ESLint errors
