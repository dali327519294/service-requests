<#
  Runs every case of measure.sql against the local database and reports, per case and variant:
  logical reads, CPU and elapsed time (median of 5 warm runs), and the operators of the actual plan.

  Usage, from the repository root (Bypass allows this one run; Windows blocks scripts by default):
    powershell -ExecutionPolicy Bypass -File docs\performance\measure.ps1 -Label after-fix
  Results go to results-<label>.json and the actual plans to plans/<label>/ (open them in SSMS).
  Nothing is changed: SELECTs only, and the UPDATE case is rolled back inside measure.sql.
#>
param(
    [string]$ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=ServiceRequests;Integrated Security=True',
    [int]$Runs = 5,
    [string]$Label = 'current'
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$plans = Join-Path (Join-Path $here 'plans') $Label
New-Item -ItemType Directory -Force $plans | Out-Null

# Blocks start with "-- @case <name> | <variant>"
$sql = Get-Content (Join-Path $here 'measure.sql') -Raw -Encoding UTF8
$blocks = [regex]::Matches($sql, '(?ms)^-- @case (?<name>[^|\r\n]+)\|\s*(?<variant>\w+)\s*$(?<body>.*?)(?=^-- @case |\z)')

$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$messages = New-Object System.Collections.Generic.List[string]
$connection.add_InfoMessage({ param($s, $e) $messages.Add($e.Message) })
$connection.Open()

function Invoke-Block([string]$body, [bool]$withPlan) {
    $messages.Clear()
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 120
    $set = if ($withPlan) { 'SET STATISTICS XML ON;' } else { '' }
    $command.CommandText = "SET STATISTICS IO, TIME ON; $set`n$body`nSET STATISTICS XML OFF; SET STATISTICS IO, TIME OFF;"
    $reader = $command.ExecuteReader()
    $planXml = New-Object System.Collections.Generic.List[string]
    do {
        # With STATISTICS XML, each statement's actual plan comes back as an extra one-column result set
        $isPlan = $reader.FieldCount -eq 1 -and $reader.GetName(0) -like 'Microsoft SQL Server*XML Showplan'
        while ($reader.Read()) { if ($isPlan) { $planXml.Add($reader.GetString(0)) } }
    } while ($reader.NextResult())
    $reader.Close()

    # Pages read from memory, for the tables themselves (worktables of sorts and spools are internal)
    $reads = 0
    foreach ($m in $messages) {
        foreach ($match in [regex]::Matches($m, "Table '(?<table>[^']+)'\. Scan count \d+, logical reads (?<reads>\d+)")) {
            if ($match.Groups['table'].Value -notlike 'Work*') { $reads += [int]$match.Groups['reads'].Value }
        }
    }

    # sp_executesql reports each inner statement and then itself, covering them all: the largest line is the block
    $cpu = 0; $elapsed = 0
    foreach ($m in $messages) {
        foreach ($match in [regex]::Matches($m, 'SQL Server Execution Times:\s+CPU time = (?<cpu>\d+) ms,\s+elapsed time = (?<el>\d+) ms')) {
            $cpu = [math]::Max($cpu, [int]$match.Groups['cpu'].Value)
            $elapsed = [math]::Max($elapsed, [int]$match.Groups['el'].Value)
        }
    }
    return [pscustomobject]@{ Reads = $reads; Cpu = $cpu; Elapsed = $elapsed; Plans = $planXml }
}

function Get-Median([int[]]$values) {
    $sorted = $values | Sort-Object
    return $sorted[[int][math]::Floor($sorted.Count / 2)]
}

# The operators that decide the cost: which index is read and how (Seek / Scan), lookups back to the table, sorts
function Get-Operators([string[]]$planXml) {
    $ops = foreach ($text in $planXml) {
        $xml = [xml]$text
        $ns = New-Object System.Xml.XmlNamespaceManager $xml.NameTable
        $ns.AddNamespace('s', 'http://schemas.microsoft.com/sqlserver/2004/07/showplan')
        foreach ($op in $xml.SelectNodes('//s:RelOp', $ns)) {
            if ($op.PhysicalOp -notmatch 'Seek|Scan|Sort|Lookup') { continue }
            $index = $op.SelectSingleNode('./*/s:Object', $ns)
            $rows = ($op.SelectNodes('s:RunTimeInformation/s:RunTimeCountersPerThread', $ns) |
                Measure-Object -Property ActualRows -Sum).Sum
            "$($op.PhysicalOp)$(if ($index) { ' ' + $index.Index.Trim('[', ']') }) ($rows rows)"
        }
    }
    return ($ops | Select-Object -Unique) -join '; '
}

$results = foreach ($block in $blocks) {
    $name = $block.Groups['name'].Value.Trim()
    $variant = $block.Groups['variant'].Value.Trim()
    $body = $block.Groups['body'].Value

    Invoke-Block $body $false | Out-Null                # warm the cache: every run below reads from memory
    $samples = 1..$Runs | ForEach-Object { Invoke-Block $body $false }
    $withPlan = Invoke-Block $body $true

    # One .sqlplan per statement (a .sqlplan holds a single plan); the count query comes first
    $slug = ($name -replace '[^\w]+', '-').Trim('-')
    for ($i = 0; $i -lt $withPlan.Plans.Count; $i++) {
        Set-Content -Path (Join-Path $plans "$slug-$variant-$($i + 1).sqlplan") -Value $withPlan.Plans[$i] -Encoding UTF8
    }

    [pscustomobject]@{
        Case      = $name
        Variant   = $variant
        Reads     = $samples[0].Reads
        CpuMs     = Get-Median ($samples | ForEach-Object Cpu)
        ElapsedMs = Get-Median ($samples | ForEach-Object Elapsed)
        Operators = Get-Operators $withPlan.Plans
    }
}

$connection.Close()
$results | Format-Table -AutoSize -Wrap
$results | ConvertTo-Json | Set-Content (Join-Path $here "results-$Label.json") -Encoding UTF8
