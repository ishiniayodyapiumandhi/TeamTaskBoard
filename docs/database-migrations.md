# MySQL migrations and recovery

## Baseline and versioning

The existing schema is the baseline, identified by `v1.0-teamtaskboard` and [database/schema.sql](../database/schema.sql). That SQL is a fresh-install reference, not an upgrade script. Phase 0 does not add a migration ledger, change tables, or connect to MySQL.

Starting in Phase 1, keep immutable, ordered SQL files under `database/migrations/`, beginning with `0001_<change>.sql`. Keep each migration's expected starting schema, validation queries, lock impact, compatibility requirements, and recovery plan in its PR. Use one migration mechanism: reviewed SQL through a separate operator identity, not EF startup migrations alongside SQL. Establish a version/checksum ledger with the first approved migration. Record the baseline for existing instances only after comparing their live schema with the baseline, and record each later version only after successful postchecks. Never edit an applied script; add a corrective migration.

### Release procedure

1. Rehearse from an isolated restore first. Compare schema, row counts, and representative workflows with the source. Take a fresh backup before changing the target.
2. Verify the destination host, schema, and expected version. Use one migration operator and an explicit deployment gate; the API must not have DDL permissions.
3. Expand first: add compatible structures, deploy code that tolerates both shapes, and backfill in bounded, restartable batches. Reconcile ownership, IDs, dates, and legacy assignments before adding constraints or switching reads.
4. Contract only in a later release after compatibility checks. MySQL DDL can implicitly commit; failure is not proof nothing changed. Stop, inspect actual schema and ledger state, and repair or roll forward. Do not automatically replay partially applied SQL or restore over live writes.

## Back up MySQL

Use MySQL 8 client tools and a backup account with only the necessary read privileges. Run from PowerShell. Passwords are entered at the MySQL prompt, not in arguments or environment files. Use an encrypted, access-controlled directory **outside** the repo; set the host for the intended source. Use verified TLS for remote connections.

```powershell
$sourceHost = 'localhost'
$backupDir = Read-Host 'Absolute path to secure backup directory outside the repository'
$backupUser = Read-Host 'MySQL backup username'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$backupName = 'team-task-board-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmssfff') + '.sql'
$dump = Join-Path $backupDir $backupName
& mysqldump.exe --no-defaults "--host=$sourceHost" "--user=$backupUser" --password `
    --single-transaction --quick --no-tablespaces --set-gtid-purged=OFF `
    "--result-file=$dump" team_task_board
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $dump) -or (Get-Item -LiteralPath $dump).Length -eq 0) {
    throw 'Backup failed or is empty. Do not use this file for recovery.'
}
Get-FileHash -LiteralPath $dump -Algorithm SHA256
```

Record the checksum, UTC time, source version, and schema version separately. Do not use `--databases` or change the command to include multiple schemas. The current tables should be InnoDB; confirm this before relying on a transactionally consistent dump, and avoid DDL during the backup. This backs up MySQL only; Firestore, Firebase configuration, and identity recovery require separate procedures.

## Rehearse a restore

Use a disposable MySQL instance and an account with privileges only on a new scratch schema. Do not use the application account, the production instance, or the existing integration-test database. Verify the checksum against the recorded value and review the dump for statements that create or switch databases or reference the application schema explicitly. Import only a trusted, verified dump. Keep the rehearsal API and Firebase project isolated from live services.

1. On the **scratch** instance, create a new empty schema: `CREATE DATABASE team_task_board_restore;`. Do not use `IF NOT EXISTS`: if it already exists, stop and select a fresh scratch instance or schema.
2. Open the MySQL client against the scratch schema. Substitute the actual scratch host, port and username:

   ```powershell
   & mysql.exe --no-defaults --host=localhost --port=3307 --user=restore_operator --password --database=team_task_board_restore
   ```

3. Confirm `SELECT DATABASE();` returns `team_task_board_restore` and `SHOW TABLES;` is empty. In that client, import the dump using `SOURCE C:/path/to/backup.sql` (use the real absolute path, forward slashes, and check for errors). Do not continue if the import reports errors.
4. Compare table definitions and row counts for `users`, `projects`, and `tasks` with the source for the same backup window. Verify project owners, task relationships, nullable dates, and a representative overdue report in isolation. Record the outcome and elapsed time. A matching checksum does not prove a usable restore.

**Verification status:** These are operator procedures. No database backup, restore, or migration was performed in Phase 0.
