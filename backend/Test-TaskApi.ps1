#Requires -Version 7
param([Parameter(Mandatory)][int] $OwnerProjectId, [string] $BaseUrl = 'https://localhost:7237',
    [switch] $CheckComments)

$ErrorActionPreference = 'Stop'
$owner = Read-Host 'Project owner Firebase ID token' -AsSecureString
$other = Read-Host 'Second account Firebase ID token' -AsSecureString

function Assert-Request($Method, $Path, $Expected, [securestring] $Token, $Body) {
    $options = @{ Method = $Method; Uri = "$BaseUrl$Path"; SkipHttpErrorCheck = $true }
    if ($Token) {
        $options.Authentication = 'Bearer'
        $options.Token = $Token
    }
    if ($Body) {
        $options.Body = $Body | ConvertTo-Json -Compress
        $options.ContentType = 'application/json'
    }
    $response = Invoke-WebRequest @options
    if ($response.StatusCode -ne $Expected) {
        throw "$Method ${Path}: expected $Expected, got $($response.StatusCode)."
    }
    if ($response.Content) { $response.Content | ConvertFrom-Json }
}

$project = "/api/projects/$OwnerProjectId"
$tasks = "$project/tasks"
$body = @{ title = "Smoke-$([guid]::NewGuid())"; priority = 'High' }
$task = $null
try {
    $null = Assert-Request GET '/api/projects' 401
    $invalid = ConvertTo-SecureString 'invalid-token' -AsPlainText -Force
    $null = Assert-Request GET '/api/projects' 401 $invalid
    $otherProjects = Assert-Request GET '/api/projects' 200 $other
    if ($OwnerProjectId -in $otherProjects.id) { throw 'The second account can list the owner project.' }
    $null = Assert-Request GET $project 200 $owner
    $null = Assert-Request GET $project 404 $other
    $null = Assert-Request GET $tasks 404 $other
    $null = Assert-Request POST $tasks 404 $other $body
    $null = Assert-Request POST $tasks 400 $owner @{title = ' '; priority = 'Unknown'}
    $null = Assert-Request POST $tasks 400 $owner @{title = 'Past due date'; dueDate = '2000-01-01'}
    $task = Assert-Request POST $tasks 201 $owner $body
    $taskPath = "/api/tasks/$($task.id)"
    $null = Assert-Request GET $taskPath 404 $other
    $null = Assert-Request PUT $taskPath 404 $other $body
    $null = Assert-Request DELETE $taskPath 404 $other
    if ($CheckComments) {
        $null = Assert-Request POST "$taskPath/comment-access" 401
        $null = Assert-Request POST "$taskPath/comment-access" 404 $other
        $null = Assert-Request POST "$taskPath/comment-access" 204 $owner
    }
    $query = "${tasks}?status=To%20Do&priority=High&search=$($body.title)&pageSize=1"
    $first = Assert-Request GET "$query&page=1" 200 $owner
    $second = Assert-Request GET "$query&page=2" 200 $owner
    if ($first.totalCount -ne 1 -or $first.items[0].id -ne $task.id -or $second.items.Count -ne 0) {
        throw 'Task filtering or pagination returned unexpected results.'
    }
    $body.status = 'Done'
    $body.description = 'Edited during smoke test'
    $updated = Assert-Request PUT $taskPath 200 $owner $body
    if ($updated.status -ne 'Done') { throw 'Task status did not change.' }
    $stored = Assert-Request GET $taskPath 200 $owner
    if ($stored.description -ne $body.description -or $stored.status -ne 'Done') {
        throw 'Task edits did not persist.'
    }
    $null = Assert-Request GET '/api/reports/overdue-by-user' 401
    $null = Assert-Request GET '/api/reports/overdue-by-user' 200 $owner
} finally {
    if ($task) { $null = Assert-Request DELETE "/api/tasks/$($task.id)" 204 $owner }
    $owner.Dispose()
    $other.Dispose()
}
Write-Output 'Task API smoke tests passed.'
