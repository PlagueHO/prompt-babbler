# Shadcn/UI Tag Components Research

## Research Topics

1. Component to DISPLAY tags as colored bubbles/badges alongside babbles
1. Component or pattern to ADD/REMOVE tags (input with autocomplete, multi-select, or similar)

## Key Discovery: Project Already Has Custom Tag Components

The project already contains custom tag UI components built on Shadcn/UI primitives:

- `prompt-babbler-app/src/components/ui/tag-list.tsx` — displays tags as badges (read-only)
- `prompt-babbler-app/src/components/ui/tag-input.tsx` — input for adding/removing tags with comma-splitting and keyboard support

Both use the `Badge` component from `@/components/ui/badge`.

---

## Component Research

### 1. Badge (Display Tags)

- **Purpose:** Displays a badge or tag-like label; ideal for showing colored tag bubbles
- **Import:** `import { Badge } from "@/components/ui/badge"`
- **Installation:** `pnpm dlx shadcn@latest add badge`
- **Already installed:** Yes (`prompt-babbler-app/src/components/ui/badge.tsx`)
- **Variants:** `default`, `secondary`, `destructive`, `outline`, `ghost`, `link`
- **Key props:**
  - `variant` — controls style (default | secondary | destructive | outline | ghost | link)
  - `className` — for custom colors (e.g., `bg-green-50 dark:bg-green-800`)
  - `asChild` — render as child element (e.g., link)
- **Tag usage pattern:**
  - Render each tag as a `<Badge variant="outline">` or `<Badge variant="secondary">`
  - Add custom color classes for category-based coloring
  - Add an `X` icon button inside badge for removable tags
  - Supports icon positioning via `data-icon="inline-start"` / `data-icon="inline-end"`

### 2. Combobox (Multi-Select Tag Input — BEST FIT for tag selection with autocomplete)

- **Purpose:** Autocomplete input with dropdown suggestions; supports multi-select with chips
- **Import:** `import { Combobox, ComboboxChip, ComboboxChips, ComboboxChipsInput, ComboboxContent, ComboboxEmpty, ComboboxInput, ComboboxItem, ComboboxList, ComboboxValue } from "@/components/ui/combobox"`
- **Installation:** `pnpm dlx shadcn@latest add combobox`
- **Already installed:** No
- **Key props:**
  - `items` — array of selectable items
  - `multiple` — enables multi-select mode
  - `value` / `onValueChange` — controlled state
  - `itemToStringValue` — for object items
  - `autoHighlight` — highlights first match on filter
  - `showClear` — shows a clear button
- **Tag usage pattern:**
  - Use `multiple` prop for selecting multiple tags
  - `ComboboxChips` + `ComboboxValue` + `ComboboxChip` renders selected tags as chips
  - `ComboboxChipsInput` provides the text input inline with chips
  - `ComboboxEmpty` shows "No items found" when filter yields nothing
  - Supports groups via `ComboboxGroup` and `ComboboxSeparator`
  - Users type to filter existing tags OR create new ones
- **Composition (multi-select with chips):**

  ```text
  Combobox
  ├── ComboboxChips
  │   ├── ComboboxValue
  │   │   └── ComboboxChip (per selected tag)
  │   └── ComboboxChipsInput
  └── ComboboxContent
      ├── ComboboxEmpty
      └── ComboboxList
          └── ComboboxItem (per suggestion)
  ```

### 3. Command (Search and Quick Actions)

- **Purpose:** Command palette / searchable list; uses `cmdk` library
- **Import:** `import { Command, CommandDialog, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList, CommandSeparator, CommandShortcut } from "@/components/ui/command"`
- **Installation:** `pnpm dlx shadcn@latest add command`
- **Already installed:** Yes (`prompt-babbler-app/src/components/ui/command.tsx`)
- **Key props:**
  - `CommandInput` — search/filter input
  - `CommandGroup` with `heading` — groups items
  - `CommandItem` — selectable item
  - `CommandEmpty` — shown when no results
- **Tag usage pattern:**
  - Could be used inside a Popover for a "tag picker" pattern
  - Less suitable for multi-select tags than Combobox (no built-in chip/multi-select)
  - Better suited for single-action command palettes
  - Historically was the recommended approach before Combobox existed in Shadcn

### 4. Input

- **Purpose:** Basic text input component
- **Import:** `import { Input } from "@/components/ui/input"`
- **Installation:** `pnpm dlx shadcn@latest add input`
- **Already installed:** Yes (`prompt-babbler-app/src/components/ui/input.tsx`)
- **Tag usage pattern:**
  - Used as the underlying input inside the existing `TagInput` component
  - Supports `Field`, `FieldLabel`, `FieldDescription` wrappers for form integration
  - Can pair with `InputGroup` for icons/addons

### 5. Popover

- **Purpose:** Displays rich content in a floating portal triggered by a button
- **Import:** `import { Popover, PopoverContent, PopoverDescription, PopoverHeader, PopoverTitle, PopoverTrigger } from "@/components/ui/popover"`
- **Installation:** `pnpm dlx shadcn@latest add popover`
- **Already installed:** No
- **Key props:**
  - `PopoverContent` with `align` — controls horizontal alignment (start | center | end)
  - Can contain form fields, commands, or any content
- **Tag usage pattern:**
  - Wrap a Command or Combobox inside a Popover for a "tag picker popup" pattern
  - Useful if tags should be edited via a button trigger rather than inline
  - API Reference at Radix UI Popover docs

### 6. Toggle Group

- **Purpose:** A set of two-state buttons that can be toggled on/off
- **Import:** `import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"`
- **Installation:** `pnpm dlx shadcn@latest add toggle-group`
- **Already installed:** No
- **Key props:**
  - `type` — "single" | "multiple"
  - `variant` — "default" | "outline"
  - `size` — sm | default | lg
  - `orientation` — horizontal | vertical
  - `spacing` — adds gaps between items
- **Tag usage pattern:**
  - Could show a fixed set of tag categories as toggleable buttons
  - Limited for free-form tags (only works with predefined options)
  - Better for filter UIs where users pick from a small fixed set

### 7. Dialog

- **Purpose:** Modal overlay window
- **Import:** `import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"`
- **Installation:** `pnpm dlx shadcn@latest add dialog`
- **Already installed:** Yes (`prompt-babbler-app/src/components/ui/dialog.tsx`)
- **Key props:**
  - `DialogContent` — main container
  - `DialogHeader` / `DialogFooter` — layout areas
  - `showCloseButton` — toggle close button
- **Tag usage pattern:**
  - Could contain a full tag editor (Combobox or TagInput) inside a modal
  - Useful for bulk tag editing on a babble
  - Pairs well with Combobox for a "manage tags" experience

---

## Recommendations for Tag Feature

### For DISPLAYING tags (read-only, alongside babbles)

**Use Badge (already implemented in TagList)**

The existing `tag-list.tsx` component uses `Badge variant="outline"` for each tag. This is the standard Shadcn/UI pattern. Custom color support is available via Tailwind classes.

### For ADDING/REMOVING tags (editable input)

**Primary recommendation: Combobox with `multiple` and chips**

- Best fit for selecting from existing tags with autocomplete
- Native multi-select with chip display (looks like tag bubbles)
- Built-in filtering, empty state, and keyboard navigation
- Supports creating new tags if combined with "create" item logic

**Alternative (already exists): Custom TagInput**

The existing `tag-input.tsx` is a custom implementation that:

- Supports comma-separated input
- Shows selected tags as Badge components with X buttons
- Handles keyboard (Enter to add, Backspace to remove, Escape to blur)
- Supports paste with comma splitting
- Has `maxTags` and `maxTagLength` constraints

**When to use which:**

| Scenario | Component |
|---|---|
| User selects from a known set of tags | Combobox (multiple) |
| User creates free-form tags | TagInput (existing) |
| User does both (select existing OR create new) | Combobox with "create new" option |
| Tag editing in a modal/popup | Dialog + Combobox or Popover + Combobox |

### Installation Commands (for components not yet installed)

```bash
pnpm dlx shadcn@latest add combobox   # Multi-select with autocomplete
pnpm dlx shadcn@latest add popover    # For popup tag picker pattern
```

---

## References

- Badge docs: <https://ui.shadcn.com/docs/components/badge>
- Combobox docs: <https://ui.shadcn.com/docs/components/combobox>
- Command docs: <https://ui.shadcn.com/docs/components/command>
- Input docs: <https://ui.shadcn.com/docs/components/input>
- Popover docs: <https://ui.shadcn.com/docs/components/popover>
- Toggle Group docs: <https://ui.shadcn.com/docs/components/toggle-group>
- Dialog docs: <https://ui.shadcn.com/docs/components/dialog>
- Existing project components: `prompt-babbler-app/src/components/ui/tag-list.tsx`, `prompt-babbler-app/src/components/ui/tag-input.tsx`, `prompt-babbler-app/src/components/ui/badge.tsx`

---

## Clarifying Questions

None — the research is self-contained. The main decision point is whether the tag feature needs autocomplete from existing tags (use Combobox) or only free-form input (existing TagInput is sufficient).
