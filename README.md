# Team Task Board

Team Task Board is a full-stack internship assignment for organizing projects, moving tasks through a Kanban board, and discussing work in realtime. Sign in to create a project, track tasks in **To Do**, **In Progress**, or **Done**, and review overdue work by assignee.

- **Frontend:** React, Vite, React Router, and responsive CSS. The board supports title search, priority filters, and pagination by status.
- **Backend:** ASP.NET Core 10 Web API with controllers, services, DTOs, and EF Core.
- **Data and identity:** MySQL 8.0 stores users, projects, and tasks. Firebase Authentication handles sign-in, and Cloud Firestore streams task comments.

## Run locally

**Prerequisites:** .NET 10 SDK, Node.js 26 with npm, MySQL 8.0, and a Firebase project. Commands below use PowerShell 7 and start from the repository root.

### 1. Prepare MySQL

If `team_task_board` is already initialized, leave it unchanged. For a fresh local setup, create it and run [schema.sql](database/schema.sql) using a MySQL administrator or another account authorized to create tables. For example, in the MySQL client:

```sql
CREATE DATABASE IF NOT EXISTS team_task_board;
SOURCE database/schema.sql;
```

Optionally run [seed.sql](database/seed.sql) once for sample rows. The seeded Firebase UIDs are placeholders, so those projects do not appear under real Firebase accounts. Create a project after signing up to try the board. Use a dedicated, non-admin MySQL account with access to the application database for the API. The API does not create, migrate, or seed tables at startup.

### 2. Configure Firebase

1. In Firebase Console, enable **Authentication → Email/Password**, add `localhost` to the authorized domains if needed, and register a Web App.
2. Create the default Cloud Firestore database and publish [firestore.rules](firestore.rules) under **Firestore Database → Rules**. Use a dedicated Firebase project: these rules deny access outside the supported comment operations.
3. Give the backend a Google Cloud identity authorized for Firestore reads and writes in the same project. Use Application Default Credentials via `gcloud auth application-default login` with an authorized account, or point `GOOGLE_APPLICATION_CREDENTIALS` to a service-account JSON stored outside the repository. Never use that JSON in the frontend.

### 3. Start the API

```powershell
$env:Firebase__ProjectId = 'your-firebase-project-id'
$env:ConnectionStrings__TaskBoard = Read-Host 'MySQL connection string' -MaskInput
dotnet dev-certs https --trust
dotnet run --project backend --launch-profile https
```

The API listens at `https://localhost:7237`. The connection string should target `Database=team_task_board`. A protected API URL opened directly in the browser returns `401` because the browser address bar does not send a Firebase token.

| Backend variable | Purpose |
| --- | --- |
| `ConnectionStrings__TaskBoard` | MySQL connection string; set privately in the API process environment. |
| `Firebase__ProjectId` | Firebase project used to validate tokens and access Firestore. |
| `Frontend__Origin` | Allowed browser origin; defaults to `http://localhost:5173`. |
| `GOOGLE_APPLICATION_CREDENTIALS` | Optional absolute path to server credentials when not using another ADC source. |

[backend/.env.example](backend/.env.example) lists the settings. ASP.NET Core does not automatically load dotenv files.

### 4. Start the web app

In a second terminal, copy the environment template:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
```

Fill in `VITE_FIREBASE_API_KEY`, `VITE_FIREBASE_AUTH_DOMAIN`, `VITE_FIREBASE_PROJECT_ID`, and `VITE_FIREBASE_APP_ID` from **Firebase Console → Project settings → Your apps**. Keep `VITE_API_URL=https://localhost:7237` for local development. Use the same Firebase project as the API. Vite exposes `VITE_*` values to the browser: never place passwords, ID tokens, or private keys in them.

```powershell
Set-Location frontend
npm ci
npm run dev
```

Open [http://localhost:5173](http://localhost:5173), sign up, and create your first project. Restart Vite after changing environment values.

## API reference

Use [Projects.http](backend/Projects.http) and [Tasks.http](backend/Tasks.http) for runnable examples. Set the relevant IDs and supply a fresh `FIREBASE_ID_TOKEN` in the ignored `backend/.env` for a compatible REST client. All routes below require `Authorization: Bearer <Firebase ID token>`.

| Method | Route | Success | Purpose |
| --- | --- | --- | --- |
| GET | `/api/projects` | 200 | List the caller's projects. |
| POST | `/api/projects` | 201 | Create a project. |
| GET | `/api/projects/{id}` | 200 | Read an owned project. |
| GET | `/api/projects/{id}/tasks` | 200 | List and filter tasks in an owned project. |
| POST | `/api/projects/{id}/tasks` | 201 | Create a task. |
| GET | `/api/tasks/{id}` | 200 | Read an owned task. |
| PUT | `/api/tasks/{id}` | 200 | Replace editable task fields, including status. |
| DELETE | `/api/tasks/{id}` | 204 | Delete an owned task. |
| POST | `/api/tasks/{id}/comment-access` | 204 | Issue or renew permission to access comments. |
| GET | `/api/reports/overdue-by-user` | 200 | Count overdue tasks by assignee across the caller's projects. |

Task lists accept `status`, `priority`, `search`, `page` (one-based), and `pageSize` (up to 100). Results contain `items`, `page`, `pageSize`, and `totalCount`. The API uses Problem Details for errors, including `400` for invalid input, `401` for missing or invalid authentication, and `404` for missing or unowned resources.

## Architecture and security

- **Identity and ownership:** React sends a Firebase ID token to the API. The API verifies the token and uses its subject as the Firebase UID. It provisions the MySQL user on first project creation and never links accounts by email. Assignees do not gain project access.
- **EF Core and MySQL:** EF Core provides typed queries and change tracking over the existing schema. The `(project_id, priority)` index supports project-scoped task lookups, including priority filters; it does not index title search. Due dates are mapped to MySQL `DATE` and exposed as date-only values. A new or changed due date cannot be in the past; keeping an existing overdue date is allowed.
- **Overdue report:** A single parameterized SQL query uses `JOIN` and `GROUP BY`. It counts only assigned, unfinished tasks due before the current UTC date, within projects the caller owns.
- **Realtime comments:** The API checks each task's MySQL ownership before issuing a two-minute Firestore access grant. Clients cannot write the grant. Firestore rules enforce the authenticated UID and grant expiry. React uses `onSnapshot` on `taskAccess/{taskId}/comments` and renews access while details are open without polling for comments.

## Tests and verification

From the repository root:

```powershell
dotnet build backend -c Release
dotnet run --project tests/BackendChecks -c Release
npm --prefix frontend test
npm --prefix frontend run build
```

**Recorded results:** Both builds passed. **25 backend checks passed**: 23 offline and 2 live MySQL checks. The database checks verified dated and undated task round-trips and that overdue counts exclude completed, future, unassigned, and undated tasks. **8 frontend tests passed.**

**Live checks passed:** Two Firebase accounts passed the API smoke test with comment-access checks. Manual checks confirmed mobile layout, logout and protected-route redirects, and two-tab realtime comments without refresh. Firestore Rules Playground denied account B's read of account A's comment and denied account A's attempt to update its own access grant.

To repeat live checks:

- **MySQL:** Initialize a fresh `team_task_board_test` database with [Initialize-TestDatabase.ps1](database/Initialize-TestDatabase.ps1), or apply a copy of the schema targeting that database only. Do not seed it. Set `TaskBoardTest__ConnectionString` at a masked prompt using `Read-Host 'Test MySQL connection string' -MaskInput`, then run `dotnet run --project tests/BackendChecks -c Release -- --mysql`. The runner only accepts the test database and rolls back fixture inserts. Remove the environment variable afterward.
- **API:** With the API running, execute `./backend/Test-TaskApi.ps1 -OwnerProjectId 123 -CheckComments`, replacing `123` with a project owned by the first test account. Enter fresh ID tokens for two distinct accounts at the masked prompts. The script creates and deletes a temporary task.
- **Realtime:** Open the same task as its owner in two browser tabs. Post in one tab and confirm the other updates without refreshing or reopening the task.

## Known limitations

- The board uses numeric MySQL assignee IDs; there is no team directory or invitation flow. The report is scoped to the caller's projects rather than a separate manager role.
- The comments panel shows the latest 100 messages. Task deletion does not remove stored comments, and an existing grant can remain valid until it expires. ID token revocation is not checked before token expiry.
- The last npm audit reported four high-severity entries tied to Firestore's transitive `@grpc/grpc-js` dependency, including [GHSA-m9gg-hp2v-232j](https://github.com/advisories/GHSA-m9gg-hp2v-232j). No compatible fix was identified in that review; no forced downgrade or dependency override was applied.

## AI usage disclosure

OpenAI Codex assisted with interpreting the assignment, implementing and reviewing the application and Firestore rules, and drafting tests and documentation. Offline and live verification are recorded separately above.
