#Requires -Version 7
param(
    [Parameter(Mandatory)][string] $AdminUser,
    [string] $MySqlHost = 'localhost',
    [int] $Port = 3306
)

$ErrorActionPreference = 'Stop'
$schema = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'schema.sql') -Raw
$expectedUse = '(?im)^\s*USE\s+team_task_board\s*;'
if ([regex]::Matches($schema, $expectedUse).Count -ne 1 -or
    [regex]::Matches($schema, '(?im)^\s*(USE|CREATE\s+DATABASE)\b').Count -ne 1) {
    throw 'Unexpected schema database directives. No SQL was executed.'
}
$tables = $schema -replace $expectedUse, ''
$sql = @"
CREATE DATABASE team_task_board_test;
USE team_task_board_test;
$tables
CREATE USER 'team_task_board_test_runner'@'localhost' IDENTIFIED BY RANDOM PASSWORD;
GRANT SELECT, INSERT ON team_task_board_test.* TO 'team_task_board_test_runner'@'localhost';
"@

& mysql.exe --no-defaults --host=$MySqlHost --port=$Port --user=$AdminUser --password --execute=$sql
if ($LASTEXITCODE -ne 0) {
    throw 'MySQL setup failed or was partially applied. Inspect only the test database before retrying.'
}
