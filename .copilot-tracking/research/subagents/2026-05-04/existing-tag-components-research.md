# Existing Tag Components Research

## Research Topics

1. How are tags currently rendered in the UI (display component)?
1. How are tags currently edited (input component)?
1. How do tags appear in BabbleCard, BabbleEditor, BabbleListItem, and BabbleBubbles?
1. What badge/styling primitives are used?
1. How do babble CRUD operations handle tags via the API?
1. Is there any color mapping or tag-specific styling logic?

---

## File Contents

### 1. `prompt-babbler-app/src/components/ui/tag-list.tsx` (20 lines)

```tsx
1  | import { Badge } from '@/components/ui/badge';
2  |
3  | interface TagListProps {
4  |   tags: string[] | undefined;
5  |   className?: string;
6  | }
7  |
8  | export function TagList({ tags, className }: TagListProps) {
9  |   if (!tags || tags.length === 0) return null;
10 |
11 |   return (
12 |     <div className={`flex flex-wrap gap-1 ${className ?? ''}`}>
13 |       {tags.map((tag) => (
14 |         <Badge key={tag} variant="outline" className="text-xs">
15 |           {tag}
16 |         </Badge>
17 |       ))}
18 |     </div>
19 |   );
20 | }
```

**Key observations:**

- Uses `Badge` with `variant="outline"` for display
- No color mapping — all tags get the same outline style
- Accepts `tags: string[] | undefined` and `className`
- Returns null if no tags

---

### 2. `prompt-babbler-app/src/components/ui/tag-input.tsx` (127 lines)

```tsx
1   | import * as React from "react"
2   | import { X } from "lucide-react"
3   | import { Badge } from "@/components/ui/badge"
4   | import { cn } from "@/lib/utils"
5   |
6   | interface TagInputProps {
7   |   value: string[]
8   |   onChange: (tags: string[]) => void
9   |   placeholder?: string
10  |   maxTags?: number
11  |   maxTagLength?: number
12  |   disabled?: boolean
13  |   id?: string
14  | }
15  |
16  | function TagInput({
17  |   value,
18  |   onChange,
19  |   placeholder = "Add a tag…",
20  |   maxTags = 20,
21  |   maxTagLength = 50,
22  |   disabled = false,
23  |   id,
24  | }: TagInputProps) {
25  |   const [inputValue, setInputValue] = React.useState("")
26  |   const inputRef = React.useRef<HTMLInputElement>(null)
27  |
28  |   const addTags = React.useCallback(
29  |     (raw: string) => {
30  |       const candidates = raw
31  |         .split(",")
32  |         .map((s) => s.trim().slice(0, maxTagLength))
33  |         .filter(Boolean)
34  |
35  |       const next = [...value]
36  |       for (const tag of candidates) {
37  |         if (next.length >= maxTags) break
38  |         if (next.some((t) => t.toLowerCase() === tag.toLowerCase())) continue
39  |         next.push(tag)
40  |       }
41  |
42  |       if (next.length !== value.length) {
43  |         onChange(next)
44  |       }
45  |     },
46  |     [value, onChange, maxTags, maxTagLength],
47  |   )
48  |
49  |   const removeTag = React.useCallback(
50  |     (index: number) => {
51  |       onChange(value.filter((_, i) => i !== index))
52  |     },
53  |     [value, onChange],
54  |   )
55  |
56  |   const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
57  |     if (e.key === "Enter") {
58  |       e.preventDefault()
59  |       if (inputValue.trim()) {
60  |         addTags(inputValue)
61  |         setInputValue("")
62  |       }
63  |     } else if (e.key === "Backspace" && inputValue === "" && value.length > 0) {
64  |       removeTag(value.length - 1)
65  |     } else if (e.key === "Escape") {
66  |       inputRef.current?.blur()
67  |     }
68  |   }
69  |
70  |   const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
71  |     const pasted = e.clipboardData.getData("text")
72  |     if (pasted.includes(",")) {
73  |       e.preventDefault()
74  |       addTags(pasted)
75  |       setInputValue("")
76  |     }
77  |   }
78  |
79  |   const atLimit = value.length >= maxTags
80  |
81  |   return (
82  |     <div
83  |       className={cn(
84  |         "border-input flex min-h-9 w-full flex-wrap items-center gap-1.5 rounded-md border bg-transparent px-3 py-1.5 shadow-xs transition-[color,box-shadow]",
85  |         "has-[input:focus]:border-ring has-[input:focus]:ring-ring/50 has-[input:focus]:ring-[3px]",
86  |         disabled && "pointer-events-none cursor-not-allowed opacity-50",
87  |       )}
88  |       onClick={() => !disabled && inputRef.current?.focus()}
89  |     >
90  |       {value.map((tag, index) => (
91  |         <Badge key={`${tag}-${index}`} variant="secondary" className="gap-1 pr-1">
92  |           {tag}
93  |           {!disabled && (
94  |             <button
95  |               type="button"
96  |               aria-label={`Remove tag: ${tag}`}
97  |               className="hover:bg-muted rounded-sm p-0.5 transition-colors"
98  |               onClick={(e) => {
99  |                 e.stopPropagation()
100 |                 removeTag(index)
101 |               }}
102 |               tabIndex={-1}
103 |             >
104 |               <X className="size-3" />
105 |             </button>
106 |           )}
107 |         </Badge>
108 |       ))}
109 |       {!disabled && !atLimit && (
110 |         <input
111 |           ref={inputRef}
112 |           id={id}
113 |           type="text"
114 |           value={inputValue}
115 |           onChange={(e) => setInputValue(e.target.value)}
116 |           onKeyDown={handleKeyDown}
117 |           onPaste={handlePaste}
118 |           onBlur={() => {
119 |             if (inputValue.trim()) {
120 |               addTags(inputValue)
121 |               setInputValue("")
122 |             }
123 |           }}
124 |           placeholder={value.length === 0 ? placeholder : ""}
125 |           className="placeholder:text-muted-foreground min-w-[80px] flex-1 bg-transparent text-sm outline-none"
126 |           aria-label="Add tag"
127 |         />
128 |       )}
129 |       {!disabled && atLimit && (
130 |         <span className="text-muted-foreground text-xs">
131 |           Maximum {maxTags} tags
132 |         </span>
133 |       )}
134 |     </div>
135 |   )
136 | }
137 |
138 | export { TagInput }
139 | export type { TagInputProps }
```

**Key observations:**

- Uses `Badge` with `variant="secondary"` for editable tags (different from display!)
- Tags support comma-separated paste input
- Case-insensitive duplicate detection
- Max 20 tags, max 50 char per tag
- Enter to add, Backspace to remove last, Escape to blur
- X button to remove individual tags
- Shows "Maximum {maxTags} tags" when limit reached

---

### 3. `prompt-babbler-app/src/components/babbles/BabbleCard.tsx` (43 lines)

```tsx
1  | import { Link } from 'react-router';
2  | import {
3  |   Card,
4  |   CardHeader,
5  |   CardTitle,
6  |   CardDescription,
7  |   CardContent,
8  | } from '@/components/ui/card';
9  | import { TagList } from '@/components/ui/tag-list';
10 | import type { Babble } from '@/types';
11 |
12 | interface BabbleCardProps {
13 |   babble: Babble;
14 | }
15 |
16 | export function BabbleCard({ babble }: BabbleCardProps) {
17 |   const dateStr = new Date(babble.updatedAt).toLocaleDateString(undefined, {
18 |     month: 'short',
19 |     day: 'numeric',
20 |     year: 'numeric',
21 |   });
22 |
23 |   const truncated =
24 |     babble.text.length > 150
25 |       ? `${babble.text.slice(0, 150)}…`
26 |       : babble.text;
27 |
28 |   return (
29 |     <Link to={`/babble/${babble.id}`} className="block">
30 |       <Card className="transition-colors hover:bg-accent/50">
31 |         <CardHeader>
32 |           <CardTitle className="text-base">{babble.title}</CardTitle>
33 |           <CardDescription>{dateStr}</CardDescription>
34 |         </CardHeader>
35 |         <CardContent>
36 |           <p className="line-clamp-3 text-sm text-muted-foreground">
37 |             {truncated || 'No content yet.'}
38 |           </p>
39 |           <TagList tags={babble.tags} className="mt-2" />
40 |         </CardContent>
41 |       </Card>
42 |     </Link>
43 |   );
44 | }
```

**Key observations:**

- Uses `<TagList>` with `className="mt-2"` in the CardContent
- Tags shown below the truncated text

---

### 4. `prompt-babbler-app/src/components/babbles/BabbleEditor.tsx` (53 lines)

```tsx
1  | import { useState } from 'react';
2  | import { Textarea } from '@/components/ui/textarea';
3  | import { Button } from '@/components/ui/button';
4  | import { TagInput } from '@/components/ui/tag-input';
5  | import { Check, X } from 'lucide-react';
6  | import type { Babble } from '@/types';
7  |
8  | interface BabbleEditorProps {
9  |   babble: Babble;
10 |   onSave: (updated: Babble) => void;
11 |   onCancel: () => void;
12 | }
13 |
14 | export function BabbleEditor({ babble, onSave, onCancel }: BabbleEditorProps) {
15 |   const [text, setText] = useState(babble.text);
16 |   const [tags, setTags] = useState<string[]>(babble.tags ?? []);
17 |
18 |   const handleSave = () => {
19 |     onSave({
20 |       ...babble,
21 |       text,
22 |       tags,
23 |       updatedAt: new Date().toISOString(),
24 |     });
25 |   };
26 |
27 |   return (
28 |     <div className="space-y-4">
29 |       <Textarea
30 |         value={text}
31 |         onChange={(e) => setText(e.target.value)}
32 |         placeholder="Babble text"
33 |         className="min-h-[200px]"
34 |       />
35 |       <div className="space-y-2">
36 |         <label htmlFor="babble-tags" className="text-sm font-medium">
37 |           Tags <span className="text-muted-foreground">(optional)</span>
38 |         </label>
39 |         <TagInput
40 |           id="babble-tags"
41 |           value={tags}
42 |           onChange={setTags}
43 |           maxTags={20}
44 |           maxTagLength={50}
45 |           placeholder="Add a tag…"
46 |         />
47 |       </div>
48 |       <div className="flex gap-2">
49 |         <Button size="sm" onClick={handleSave}>
50 |           <Check className="size-4" />
51 |           Save
52 |         </Button>
53 |         <Button size="sm" variant="outline" onClick={onCancel}>
54 |           <X className="size-4" />
55 |           Cancel
56 |         </Button>
57 |       </div>
58 |     </div>
59 |   );
60 | }
```

**Key observations:**

- Uses `<TagInput>` for editing tags
- Initializes from `babble.tags ?? []`
- On save, spreads existing babble with new `text`, `tags`, and `updatedAt`
- Tags are optional (`string[]`)

---

### 5. `prompt-babbler-app/src/components/babbles/BabbleListItem.tsx` (56 lines)

```tsx
1  | import { Link } from 'react-router';
2  | import { Pin, PinOff } from 'lucide-react';
3  | import { Button } from '@/components/ui/button';
4  | import { TagList } from '@/components/ui/tag-list';
5  | import { cn } from '@/lib/utils';
6  | import type { Babble } from '@/types';
7  |
8  | interface BabbleListItemProps {
9  |   babble: Babble;
10 |   onTogglePin: (babbleId: string) => void;
11 | }
12 |
13 | export function BabbleListItem({ babble, onTogglePin }: BabbleListItemProps) {
14 |   const dateStr = new Date(babble.createdAt).toLocaleDateString(undefined, {
15 |     month: 'short',
16 |     day: 'numeric',
17 |     year: 'numeric',
18 |   });
19 |
20 |   return (
21 |     <div
22 |       className={cn(
23 |         'flex items-center gap-3 rounded-md border px-4 py-3 transition-colors hover:bg-accent/50',
24 |         babble.isPinned && 'border-primary/30 bg-primary/5',
25 |       )}
26 |     >
27 |       <div className="min-w-0 flex-1">
28 |         <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:gap-3">
29 |           <Link
30 |             to={`/babble/${babble.id}`}
31 |             className="truncate text-sm font-medium hover:underline"
32 |           >
33 |             {babble.title}
34 |           </Link>
35 |           <span className="shrink-0 text-xs text-muted-foreground">{dateStr}</span>
36 |         </div>
37 |         {babble.tags && babble.tags.length > 0 && (
38 |           <TagList tags={babble.tags} className="mt-1" />
39 |         )}
40 |       </div>
41 |       <Button
42 |         variant="ghost"
43 |         size="icon"
44 |         className={cn(
45 |           'size-7 shrink-0 text-muted-foreground hover:text-foreground',
46 |           babble.isPinned && 'text-primary',
47 |         )}
48 |         onClick={() => onTogglePin(babble.id)}
49 |         aria-label={babble.isPinned ? 'Unpin babble' : 'Pin babble'}
50 |       >
51 |         {babble.isPinned ? (
52 |           <Pin className="size-3.5 fill-current" />
53 |         ) : (
54 |           <PinOff className="size-3.5" />
55 |         )}
56 |       </Button>
57 |     </div>
58 |   );
59 | }
```

**Key observations:**

- Uses `<TagList>` with `className="mt-1"` below the title/date row
- Conditionally renders only if `babble.tags && babble.tags.length > 0`

---

### 6. `prompt-babbler-app/src/components/babbles/BabbleBubbles.tsx` (87 lines)

```tsx
1  | import { Link } from 'react-router';
2  | import { Pin, PinOff } from 'lucide-react';
3  | import { Button } from '@/components/ui/button';
4  | import {
5  |   Card,
6  |   CardHeader,
7  |   CardTitle,
8  |   CardDescription,
9  |   CardContent,
10 | } from '@/components/ui/card';
11 | import { TagList } from '@/components/ui/tag-list';
12 | import { cn } from '@/lib/utils';
13 | import type { Babble } from '@/types';
14 |
15 | interface BabbleBubblesProps {
16 |   babbles: Babble[];
17 |   onTogglePin: (babbleId: string) => void;
18 | }
19 |
20 | export function BabbleBubbles({ babbles, onTogglePin }: BabbleBubblesProps) {
21 |   return (
22 |     <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
23 |       {babbles.map((babble) => (
24 |         <BabbleBubbleCard key={babble.id} babble={babble} onTogglePin={onTogglePin} />
25 |       ))}
26 |     </div>
27 |   );
28 | }
29 |
30 | interface BabbleBubbleCardProps {
31 |   babble: Babble;
32 |   onTogglePin: (babbleId: string) => void;
33 | }
34 |
35 | function BabbleBubbleCard({ babble, onTogglePin }: BabbleBubbleCardProps) {
36 |   const dateStr = new Date(babble.updatedAt).toLocaleDateString(undefined, {
37 |     month: 'short',
38 |     day: 'numeric',
39 |     year: 'numeric',
40 |   });
41 |
42 |   const truncated =
43 |     babble.text.length > 150 ? `${babble.text.slice(0, 150)}…` : babble.text;
44 |
45 |   return (
46 |     <div className="relative group">
47 |       <Link to={`/babble/${babble.id}`} className="block">
48 |         <Card
49 |           className={cn(
50 |             'transition-colors hover:bg-accent/50',
51 |             babble.isPinned && 'border-primary/30 bg-primary/5',
52 |           )}
53 |         >
54 |           <CardHeader className="pr-10">
55 |             <CardTitle className="text-base">{babble.title}</CardTitle>
56 |             <CardDescription>{dateStr}</CardDescription>
57 |           </CardHeader>
58 |           <CardContent>
59 |             <p className="line-clamp-3 text-sm text-muted-foreground">
60 |               {truncated || 'No content yet.'}
61 |             </p>
62 |             <TagList tags={babble.tags} className="mt-2" />
63 |           </CardContent>
64 |         </Card>
65 |       </Link>
66 |       <Button
67 |         variant="ghost"
68 |         size="icon"
69 |         className={cn(
70 |           'absolute right-2 top-2 size-7 opacity-0 group-hover:opacity-100 transition-opacity',
71 |           babble.isPinned && 'opacity-100 text-primary',
72 |         )}
73 |         onClick={(e) => {
74 |           e.preventDefault();
75 |           onTogglePin(babble.id);
76 |         }}
77 |         aria-label={babble.isPinned ? 'Unpin babble' : 'Pin babble'}
78 |       >
79 |         {babble.isPinned ? (
80 |           <Pin className="size-3.5 fill-current" />
81 |         ) : (
82 |           <PinOff className="size-3.5" />
83 |         )}
84 |       </Button>
85 |     </div>
86 |   );
87 | }
```

**Key observations:**

- Uses `<TagList>` with `className="mt-2"` in CardContent
- Same pattern as BabbleCard

---

### 7. `prompt-babbler-app/src/components/ui/badge.tsx` (50 lines)

```tsx
1  | import * as React from "react"
2  | import { cva, type VariantProps } from "class-variance-authority"
3  | import { Slot } from "radix-ui"
4  |
5  | import { cn } from "@/lib/utils"
6  |
7  | const badgeVariants = cva(
8  |   "inline-flex items-center justify-center rounded-full border border-transparent px-2 py-0.5 text-xs font-medium w-fit whitespace-nowrap shrink-0 [&>svg]:size-3 gap-1 [&>svg]:pointer-events-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px] aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive transition-[color,box-shadow] overflow-hidden",
9  |   {
10 |     variants: {
11 |       variant: {
12 |         default: "bg-primary text-primary-foreground [a&]:hover:bg-primary/90",
13 |         secondary:
14 |           "bg-secondary text-secondary-foreground [a&]:hover:bg-secondary/90",
15 |         destructive:
16 |           "bg-destructive text-white [a&]:hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:focus-visible:ring-destructive/40 dark:bg-destructive/60",
17 |         outline:
18 |           "border-border text-foreground [a&]:hover:bg-accent [a&]:hover:text-accent-foreground",
19 |         ghost: "[a&]:hover:bg-accent [a&]:hover:text-accent-foreground",
20 |         link: "text-primary underline-offset-4 [a&]:hover:underline",
21 |       },
22 |     },
23 |     defaultVariants: {
24 |       variant: "default",
25 |     },
26 |   }
27 | )
28 |
29 | function Badge({
30 |   className,
31 |   variant = "default",
32 |   asChild = false,
33 |   ...props
34 | }: React.ComponentProps<"span"> &
35 |   VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
36 |   const Comp = asChild ? Slot.Root : "span"
37 |
38 |   return (
39 |     <Comp
40 |       data-slot="badge"
41 |       data-variant={variant}
42 |       className={cn(badgeVariants({ variant }), className)}
43 |       {...props}
44 |     />
45 |   )
46 | }
47 |
48 | export { Badge, badgeVariants }
```

**Key observations:**

- Standard shadcn/ui badge with CVA
- 6 variants: `default`, `secondary`, `destructive`, `outline`, `ghost`, `link`
- `rounded-full` (pill shape)
- No tag-specific color logic built in
- The component accepts `className` for additional styling

---

### 8. `prompt-babbler-app/src/hooks/useBabbles.ts` (272 lines)

```tsx
1   | import { useState, useCallback, useEffect, useRef } from 'react';
2   | import type { Babble } from '@/types';
3   | import * as api from '@/services/api-client';
4   | import { isAuthConfigured } from '@/auth/authConfig';
5   | import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';
6   | import { isMigrationNeeded, migrateLocalBabbles } from '@/services/migration';
7   |
8   | const BUBBLES_PAGE_SIZE = 6;
9   | const LIST_PAGE_SIZE = 20;
10  |
11  | export function useBabbles() {
12  |   // Bubbles state (top section — always pinned first then recent, not affected by search/sort)
13  |   const [bubbleBabbles, setBubbleBabbles] = useState<Babble[]>([]);
14  |   const [bubblesLoading, setBubblesLoading] = useState(true);
15  |
16  |   // List state (filtered/sorted, paginated)
17  |   const [listBabbles, setListBabbles] = useState<Babble[]>([]);
18  |   const [listLoading, setListLoading] = useState(true);
19  |   const [listContinuationToken, setListContinuationToken] = useState<string | null>(null);
20  |   const [listHasMore, setListHasMore] = useState(false);
21  |   const [loadingMore, setLoadingMore] = useState(false);
22  |
23  |   // Filter/sort state for list
24  |   const [search, setSearch] = useState('');
25  |   const [sortBy, setSortBy] = useState<'createdAt' | 'title'>('createdAt');
26  |   const [sortDirection, setSortDirection] = useState<'desc' | 'asc'>('desc');
27  |
28  |   // General
29  |   const [error, setError] = useState<string | null>(null);
30  |
31  |   const getAuthToken = useAuthToken();
32  |   const accountCount = useAccountCount();
33  |   const migrationDone = useRef(false);
34  |
35  |   // Stabilize getAuthToken reference
36  |   const getAuthTokenRef = useRef(getAuthToken);
37  |   getAuthTokenRef.current = getAuthToken;
38  |
39  |   // Fetch bubbles: pinned first, then recent, up to 6 total
40  |   const fetchBubbles = useCallback(async () => {
41  |     try {
42  |       setBubblesLoading(true);
43  |       const authToken = await getAuthTokenRef.current();
44  |       if (isAuthConfigured && !authToken) {
45  |         setBubbleBabbles([]);
46  |         return;
47  |       }
48  |
49  |       // Fetch pinned babbles and recent non-pinned in parallel
50  |       const [pinnedResult, recentResult] = await Promise.all([
51  |         api.getBabbles(
52  |           { isPinned: true, sortBy: 'createdAt', sortDirection: 'desc', pageSize: BUBBLES_PAGE_SIZE },
53  |           authToken,
54  |         ),
55  |         api.getBabbles(
56  |           { isPinned: false, sortBy: 'createdAt', sortDirection: 'desc', pageSize: BUBBLES_PAGE_SIZE },
57  |           authToken,
58  |         ),
59  |       ]);
60  |
61  |       // Combine: pinned first, then non-pinned, up to 6
62  |       const pinned = pinnedResult.items;
63  |       const nonPinned = recentResult.items;
64  |       const bubbles = [...pinned, ...nonPinned].slice(0, BUBBLES_PAGE_SIZE);
65  |       setBubbleBabbles(bubbles);
66  |     } catch (err) {
67  |       setError(err instanceof Error ? err.message : 'Failed to load babbles');
68  |     } finally {
69  |       setBubblesLoading(false);
70  |     }
71  |   }, []);
72  |
73  |   // Fetch list section (filtered/sorted)
74  |   const fetchList = useCallback(async (
75  |     searchVal: string,
76  |     sortByVal: 'createdAt' | 'title',
77  |     sortDirVal: 'desc' | 'asc',
78  |     append = false,
79  |     token?: string | null,
80  |   ) => {
81  |     try {
82  |       if (append) {
83  |         setLoadingMore(true);
84  |       } else {
85  |         setListLoading(true);
86  |       }
87  |       setError(null);
88  |       const authToken = await getAuthTokenRef.current();
89  |       if (isAuthConfigured && !authToken) {
90  |         setListBabbles([]);
91  |         return;
92  |       }
93  |
94  |       const data = await api.getBabbles(
95  |         {
96  |           search: searchVal || undefined,
97  |           sortBy: sortByVal,
98  |           sortDirection: sortDirVal,
99  |           pageSize: LIST_PAGE_SIZE,
100 |           continuationToken: append ? token : null,
101 |         },
102 |         authToken,
103 |       );
104 |       setListBabbles((prev) => append ? [...prev, ...data.items] : data.items);
105 |       setListContinuationToken(data.continuationToken);
106 |       setListHasMore(!!data.continuationToken);
107 |     } catch (err) {
108 |       setError(err instanceof Error ? err.message : 'Failed to load babbles');
109 |     } finally {
110 |       setListLoading(false);
111 |       setLoadingMore(false);
112 |     }
113 |   }, []);
114 |
115 |   const didMount = useRef(false);
116 |
117 |   // Initial load
118 |   useEffect(() => {
119 |     if (isAuthConfigured && accountCount === 0) {
120 |       setBubbleBabbles([]);
121 |       setListBabbles([]);
122 |       setBubblesLoading(false);
123 |       setListLoading(false);
124 |       didMount.current = true;
125 |     } else {
126 |       const doInit = async () => {
127 |         if (!migrationDone.current && isMigrationNeeded()) {
128 |           const authToken = await getAuthTokenRef.current();
129 |           await migrateLocalBabbles(authToken);
130 |           migrationDone.current = true;
131 |         }
132 |         await Promise.all([
133 |           fetchBubbles(),
134 |           fetchList('', 'createdAt', 'desc'),
135 |         ]);
136 |         didMount.current = true;
137 |       };
138 |       void doInit();
139 |     }
140 |   }, [fetchBubbles, fetchList, accountCount]);
141 |
142 |   // Refetch list when search/sort changes (skip on initial mount)
143 |   useEffect(() => {
144 |     if (!didMount.current) return;
145 |     void fetchList(search, sortBy, sortDirection);
146 |   }, [search, sortBy, sortDirection, fetchList]);
147 |
148 |   const loadMore = useCallback(() => {
149 |     if (listHasMore && listContinuationToken && !loadingMore) {
150 |       void fetchList(search, sortBy, sortDirection, true, listContinuationToken);
151 |     }
152 |   }, [fetchList, listHasMore, listContinuationToken, loadingMore, search, sortBy, sortDirection]);
153 |
154 |   // Toggle pin with optimistic update
155 |   const togglePin = useCallback(async (babbleId: string) => {
156 |     // Find in bubbles or list
157 |     const allBabbles = [...bubbleBabbles, ...listBabbles];
158 |     const babble = allBabbles.find((b) => b.id === babbleId);
159 |     if (!babble) return;
160 |
161 |     const newIsPinned = !babble.isPinned;
162 |
163 |     // Optimistic update
164 |     setBubbleBabbles((prev) => {
165 |       const updated = prev.map((b) => b.id === babbleId ? { ...b, isPinned: newIsPinned } : b);
166 |       // Re-sort: pinned first, then by createdAt desc
167 |       return [...updated].sort((a, b) => {
168 |         if (a.isPinned !== b.isPinned) return a.isPinned ? -1 : 1;
169 |         return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
170 |       });
171 |     });
172 |     setListBabbles((prev) =>
173 |       prev.map((b) => b.id === babbleId ? { ...b, isPinned: newIsPinned } : b)
174 |     );
175 |
176 |     try {
177 |       const authToken = await getAuthTokenRef.current();
178 |       await api.pinBabble(babbleId, newIsPinned, authToken);
179 |       // Refresh bubbles to reflect new pin state accurately
180 |       void fetchBubbles();
181 |     } catch {
182 |       // Revert optimistic update on error
183 |       setBubbleBabbles((prev) =>
184 |         prev.map((b) => b.id === babbleId ? { ...b, isPinned: babble.isPinned } : b)
185 |       );
186 |       setListBabbles((prev) =>
187 |         prev.map((b) => b.id === babbleId ? { ...b, isPinned: babble.isPinned } : b)
188 |       );
189 |     }
190 |   }, [bubbleBabbles, listBabbles, fetchBubbles]);
191 |
192 |   const createBabble = useCallback(
193 |     async (request: { title: string; text: string; tags?: string[] }): Promise<Babble> => {
194 |       const authToken = await getAuthTokenRef.current();
195 |       const created = await api.createBabble(request, authToken);
196 |       // Refresh both sections
197 |       void fetchBubbles();
198 |       void fetchList(search, sortBy, sortDirection);
199 |       return created;
200 |     },
201 |     [fetchBubbles, fetchList, search, sortBy, sortDirection],
202 |   );
203 |
204 |   const updateBabble = useCallback(
205 |     async (id: string, request: { title: string; text: string; tags?: string[] }): Promise<Babble> => {
206 |       const authToken = await getAuthTokenRef.current();
207 |       const updated = await api.updateBabble(id, request, authToken);
208 |       setBubbleBabbles((prev) => prev.map((b) => (b.id === id ? updated : b)));
209 |       setListBabbles((prev) => prev.map((b) => (b.id === id ? updated : b)));
210 |       return updated;
211 |     },
212 |     [],
213 |   );
214 |
215 |   const deleteBabble = useCallback(
216 |     async (id: string): Promise<void> => {
217 |       const authToken = await getAuthTokenRef.current();
218 |       await api.deleteBabble(id, authToken);
219 |       setBubbleBabbles((prev) => prev.filter((b) => b.id !== id));
220 |       setListBabbles((prev) => prev.filter((b) => b.id !== id));
221 |     },
222 |     [],
223 |   );
224 |
225 |   const getBabble = useCallback(
226 |     async (id: string): Promise<Babble | null> => {
227 |       try {
228 |         const authToken = await getAuthTokenRef.current();
229 |         return await api.getBabble(id, authToken);
230 |       } catch {
231 |         return null;
232 |       }
233 |     },
234 |     [],
235 |   );
236 |
237 |   const loading = bubblesLoading && listLoading;
238 |   const totalBabbles = bubbleBabbles.length + listBabbles.length;
239 |
240 |   return {
241 |     // Bubbles section
242 |     bubbleBabbles,
243 |     bubblesLoading,
244 |     // List section
245 |     listBabbles,
246 |     listLoading,
247 |     listHasMore,
248 |     loadingMore,
249 |     loadMore,
250 |     // Filter/sort
251 |     search,
252 |     setSearch,
253 |     sortBy,
254 |     setSortBy,
255 |     sortDirection,
256 |     setSortDirection,
257 |     // General
258 |     loading,
259 |     error,
260 |     totalBabbles,
261 |     // Actions
262 |     togglePin,
263 |     createBabble,
264 |     updateBabble,
265 |     deleteBabble,
266 |     getBabble,
267 |     refresh: () => {
268 |       void fetchBubbles();
269 |       void fetchList(search, sortBy, sortDirection);
270 |     },
271 |   };
272 | }
```

**Key observations:**

- `createBabble` accepts `{ title: string; text: string; tags?: string[] }`
- `updateBabble` accepts `{ title: string; text: string; tags?: string[] }`
- Both call `api.createBabble` / `api.updateBabble` from `@/services/api-client`
- Tags are passed through as `string[]` — no transformation

---

## Color Mapping / Tag Styling Analysis

**Result: NO color mapping or tag-specific styling exists in the codebase.**

Searches performed:

- `tag.*color|color.*tag|tagColor|tag-color` — no source matches (only dist build artifacts)
- `getTagColor|tagColors|TAG_COLOR` — no matches
- `tag.*class|tag.*style|tag.*variant|colorMap|colorFor` — only found the standard badge variant usage

**Current tag rendering uses:**

- **Display context** (`TagList`): `Badge variant="outline"` — renders with `border-border text-foreground`
- **Edit context** (`TagInput`): `Badge variant="secondary"` — renders with `bg-secondary text-secondary-foreground`
- **Search results** (`SearchCommand`): `Badge variant="secondary"` with `className="text-xs"`

All tags are rendered with identical styling regardless of their content. There is no hash-based color assignment, no predefined color palette, and no per-tag color differentiation.

---

## Additional Usage Sites for Tags

| Component | File | Usage |
|-----------|------|-------|
| `SearchCommand` | `src/components/search/SearchCommand.tsx` (line 75) | `Badge variant="secondary"` for first 3 tags |
| `TemplateCard` | `src/components/templates/TemplateCard.tsx` (line 36) | `TagList` for template tags |
| `TemplateEditor` | `src/components/templates/TemplateEditor.tsx` (line 294) | `TagInput` for template tag editing |

---

## API Integration Summary

Tags flow through the system as `string[]`:

1. **Create**: `api.createBabble({ title, text, tags? })` → POST `/api/babbles`
1. **Update**: `api.updateBabble(id, { title, text, tags? })` → PUT `/api/babbles/:id`
1. **Read**: Tags come back on the `Babble` type from GET responses
1. **No separate tag endpoints** — tags are always part of the babble payload

---

## Key Discoveries

1. **Two distinct badge variants** are used for tags:
   - `outline` for read-only display (TagList)
   - `secondary` for editable display (TagInput, SearchCommand)

1. **No color differentiation** — all tags look identical regardless of content

1. **Tag constraints** are enforced client-side: max 20 tags, max 50 chars, case-insensitive dedup

1. **TagList renders nothing** when tags is undefined or empty (null return)

1. **BabbleEditor passes tags as optional** in the save payload — the `updatedAt` is set client-side

1. **The Badge component** uses CVA with `rounded-full` (pill shape) and supports 6 variants
