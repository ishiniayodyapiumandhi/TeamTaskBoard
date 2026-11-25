# TeamFlow roadmap

| Phase | Scope | Exit gate |
| --- | --- | --- |
| 0 · Foundation | Preserve the Team Task Board baseline; introduce TeamFlow branding, offline CI, migration policy, and recovery procedures. | Local checks pass; CI passes once run on GitHub; backup and restore rehearsal is recorded before applying migrations. |
| 1 · Personal workspaces | Add workspace and membership structures incrementally. Backfill personal workspaces without sharing, preserving owners, task IDs, and comment paths. | Before/after reconciliation on a restored copy; unchanged access and data; reversible compatibility deployment. |
| 2 · Tenant isolation | Make resource policies workspace-aware. Add eligible assignee lookup and optimistic task concurrency. | Automated permission matrix denies cross-tenant routes, reporting, assignments, and comment grants. Resolve legacy out-of-workspace assignments explicitly. |
| 3 · Collaboration | Add invitations, role management, multi-user comments, audit events and durable revocation. Roll out behind a feature flag. | Verify concurrent acceptance, viewer restrictions, last-owner protection, and member removal with a live listener. |
| 4 · Production readiness | Stage on isolated infrastructure, separate runtime and migration identities, add observability, dependency review, and release/restore drills. | Successful staging exercises for clean setup, upgrades, rollback, backup recovery, and multi-user workflows. |

## Security follow-ups

- The previous npm audit found four high-severity entries in the Firebase/Firestore dependency chain that includes `@grpc/grpc-js`. See [GHSA-m9gg-hp2v-232j](https://github.com/advisories/GHSA-m9gg-hp2v-232j). Re-audit the lockfile and deployed artifact before release; validate a supported upstream fix rather than forcing a downgrade.
- Rotate the previously exposed MySQL test account password before further use. Use an administrator session with a masked password prompt, rotate only the dedicated test account, and store the replacement in a password manager. Never copy it into a template, issue, or Git history. Rerun the isolated MySQL checks with the replacement.
- Keep runtime identities least-privileged and isolate Firebase projects and MySQL environments. CI runs no database migrations, live smoke tests, or Firebase operations.
