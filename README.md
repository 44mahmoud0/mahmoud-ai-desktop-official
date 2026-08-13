# Still / Signal

Still / Signal is an English-first editorial magazine experience for thoughtful stories, field notes, and a low-noise reading rhythm. The public product uses a custom editorial layout rather than an internal dashboard pattern. The current identity is original and distinct from the supplied prototype; the prototype and PDF were used only as design references.

## Product scope

The public experience includes a feature story, latest-story grid, categories, keyword search, article reading pages, related stories, newsletter signup, loading states, empty states, and error states. The owner workspace provides authenticated article management with create, edit, publish or draft, feature selection, and guarded deletion. Editorial writes are protected on the server by the configured owner identity, not only by client-side navigation.

The default language is English and the default document direction is LTR. Arabic and RTL support is documented as a future localization boundary rather than being forced into the English-first experience. The design system uses logical layout decisions where practical so a later direction switch can be added without rebuilding the publication.

## Technology

The project uses React, Vite, Tailwind CSS, Express, tRPC, Drizzle ORM, MySQL-compatible database access, Manus OAuth, and Vitest. Public and owner operations use typed tRPC procedures. Route-level code splitting keeps the article and owner desk pages out of the initial homepage route chunk.

## Local development

Install dependencies with:

```bash
pnpm install
```

Start the development server with:

```bash
pnpm dev
```

Run the static TypeScript check with:

```bash
pnpm check
```

Format the repository with:

```bash
pnpm format
```

Run the complete test suite with:

```bash
pnpm test
```

Create a production build with:

```bash
pnpm build
```

## Database workflow

The canonical schema lives in `drizzle/schema.ts`. Generate migrations with:

```bash
pnpm drizzle-kit generate
```

Review the generated SQL before applying it. In the managed project workflow, apply schema changes through the database migration execution flow rather than destructive local shortcuts. The current schema includes users, articles, and newsletter subscriptions. Article slugs and newsletter emails are unique, and article queries support publication status, category, featured selection, and normalized search.

## Important files

| Area | Location | Responsibility |
| --- | --- | --- |
| Public route shell | `client/src/App.tsx` | Routes, lazy loading, theme, global providers |
| Homepage | `client/src/pages/Home.tsx` | Hero story, latest stories, search, categories, newsletter |
| Reading page | `client/src/pages/ArticleDetail.tsx` | Article metadata, cover, long-form content, related stories |
| Owner desk | `client/src/pages/AdminPanel.tsx` | Owner-only article CRUD and guarded deletion |
| Editorial components | `client/src/components/editorial/` | Cover art and reusable story cards |
| Domain model | `domain-model.md` | Article, category, newsletter, and permission vocabulary |
| Product identity | `brand-identity.md` | Still / Signal positioning, language policy, visual direction |
| Information architecture | `information-architecture.md` | Routes, states, and navigation flows |
| Database schema | `drizzle/schema.ts` | Users, articles, and newsletter subscriptions |
| Database helpers | `server/db.ts` | Queries, mutations, normalization, conflicts, safe logs |
| API contracts | `server/routers.ts` | Public and owner-only typed procedures |
| Tests | `server/*.test.ts` | Auth, validation, authorization, CRUD, conflicts, and search |
| Research log | `research_notes.md` | PDF, GitHub, runtime, visual, browser, and security findings |
| Work tracking | `todo.md` | Granular implementation and verification checklist |

## Owner access

The owner desk is available at `/admin`. The UI reports loading, unauthenticated, and forbidden states. The server additionally requires the authenticated user to match the configured owner identity for every editorial write. Deletion requires both the article ID and the expected current slug, protecting against stale confirmation dialogs deleting a different record after a list has changed.

## Content and media policy

The sample stories are original English editorial content intended to make the preview useful without fabricating customer reviews, ratings, or testimonials. Cover art supports real image URLs and retains an abstract field-note fallback when no URL is available. Production media should follow the project storage policy and should not be committed as large local assets.

## Verification summary

The project has been checked with TypeScript, Vitest, and a production build. The live preview was inspected at desktop and mobile sizes for the homepage, article detail, owner desk, search, category filtering, newsletter success and invalid-email validation, loading and empty states, and route-level lazy loading. Runtime and network logs were reviewed without copying session tokens into project notes. The build still reports a non-blocking advisory for a large main chunk; article and owner routes now emit separate chunks.

## Future-facing work

RTL and Arabic localization remains an explicit future feature rather than a hidden partial implementation. A future direction switch should include translated copy boundaries, logical CSS verification, route-by-route visual review, and tests for mixed-direction metadata. Additional production work may include a richer media workflow, editorial scheduling, analytics dashboards, moderation, and a more granular role model.

