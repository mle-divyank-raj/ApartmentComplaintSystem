# Start the Web App (ResidentApp.Web)

**Project:** `apps/ResidentApp.Web/`  
**Framework:** Next.js 14 / React 18 / TypeScript / Tailwind CSS  
**Default URL:** `http://localhost:3000`  
**Users:** Property managers and admin staff (browser-based dashboard)

---

## Prerequisites

- Node.js 20 LTS — `node --version`
- npm 10.x — `npm --version`
- Backend API running on `http://localhost:5000` — see [Start the Backend](startBackend.md)

---

## Step 1 — Create the Environment File

Create `apps/ResidentApp.Web/.env.local` (this file is gitignored):

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
```

This variable is the only runtime configuration the web app reads. It is consumed by
`packages/sdk/src/apiClient.ts` for all HTTP calls.

---

## Step 2 — Install Dependencies

From the **repository root** (this is an npm workspace monorepo):

```bash
npm install
```

This installs dependencies for all workspaces, including the shared packages
(`@acls/sdk`, `@acls/shared-types`, `@acls/error-codes`, `@acls/api-contracts`).

---

## Step 3 — Build Shared Packages

The web app depends on packages in `packages/`. Build them first:

```bash
npm run build:packages
```

---

## Step 4 — Start the Development Server

```bash
# From the repo root:
npm run dev

# Or directly from the app directory:
cd apps/ResidentApp.Web
npm run dev
```

Open `http://localhost:3000` in your browser.

The dev server supports Hot Module Replacement (HMR) — changes to source files are
reflected in the browser without a full page reload.

---

## Other Useful Commands

| Command | What it does |
|---|---|
| `npm run build` (from root) | Production build |
| `npm start` (from `apps/ResidentApp.Web`) | Serve the production build on port 3000 |
| `cd apps/ResidentApp.Web && npm run typecheck` | TypeScript type-check without emitting output |

---

## Troubleshooting

**Cannot connect to API (`Network Error` in browser console)**  
Verify the backend is running: `curl http://localhost:5000/healthz`

**`Module not found: @acls/sdk`**  
Run `npm run build:packages` from the repo root — shared packages must be built before the app can import them.

**Port 3000 already in use**  
Pass a different port: `npm run dev -- -p 3001`
