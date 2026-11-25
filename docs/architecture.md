# TeamFlow architecture

## Phase 0 boundary

TeamFlow retains a React SPA, an ASP.NET Core API, one MySQL database, and Firebase Authentication and Firestore. Firebase establishes identity; MySQL currently authorizes by project owner. The browser uses Firestore snapshots for comments after the API verifies ownership and issues a short-lived grant. No workspace tables, collaboration endpoints, or permission changes are included in Phase 0.

## Target modular monolith

Keep one API deployment, one EF Core context, and one ordered MySQL migration history at first. Organize backend code by capability; each module owns writes to its data and exposes operations to other modules. Use transactions for changes within MySQL and a durable outbox for email and cross-store side effects. Do not split services or databases without a measured need.

| Module | Responsibility |
| --- | --- |
| Identity | Firebase principal validation and idempotent local user provisioning by UID. |
| Workspaces | Membership, roles, invitations, and resource authorization policies. |
| Projects | Workspace-scoped project lifecycle and queries. |
| Tasks | Board reads, task commands, eligible assignments, and edit conflicts. |
| Comments | Comment authorization, Firestore adapter, grant lifecycle and revocation. |
| Reporting | Workspace-scoped reporting queries and read models. |

Proposed folders: `backend/Modules/<Capability>`, `backend/Infrastructure/{Persistence,Messaging,Observability}`, `frontend/src/app`, `frontend/src/features/<capability>`, and `frontend/src/shared`. Move code incrementally behind existing routes and regression checks rather than restructuring the entire repository at once.

## Proposed permissions

Projects are shared with active members of their workspace in the first collaboration release. Roles are scoped to each workspace, not global Firebase claims.

| Capability | Owner | Admin | Member | Viewer |
| --- | --- | --- | --- | --- |
| Read projects, tasks and comments | Yes | Yes | Yes | Yes |
| Create or edit tasks, assign eligible members, post comments | Yes | Yes | Yes | No |
| Delete tasks, manage projects, view workspace-wide reports | Yes | Yes | No | No |
| Invite or manage non-owner members within role limits | Yes | Yes | No | No |
| Transfer ownership or archive the workspace | Yes | No | No | No |

Never remove the last owner. Check active membership and capability server-side for each resource; a workspace ID in the URL is not proof of access. Accept invitations once, in a transaction, using expiring hashed tokens and a matching verified email. Confirm that assignees belong to the workspace. Preserve historical attribution when membership ends.

The existing single-owner comment grant is not safe to reuse for multiple members. Before sharing, design per-user, capability-scoped grants and test reads, writes, renewals, and already-open listeners during member removal. MySQL and Firestore do not commit together. Define an acceptable revocation window; if direct Firestore grants cannot meet it, use API-mediated realtime authorization instead.
