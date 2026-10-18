Set-StrictMode -Version Latest

$script:Invariant = [System.Globalization.CultureInfo]::InvariantCulture
$script:JsonWriteOptions = [System.Text.Json.JsonSerializerOptions]::new()
$script:JsonWriteOptions.Encoder = [System.Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping

$script:FigureFamilies = @(
    'banks', 'cards', 'categories', 'incomes', 'tithe', 'snapshots', 'monthly', 'pots', 'current', 'annual', 'averages', 'investment'
)

function Get-ParityFigureFamilies { $script:FigureFamilies }

function ConvertFrom-ParityJsonElement([System.Text.Json.JsonElement]$Element) {
    switch ($Element.ValueKind) {
        'Object' {
            $result = [ordered]@{}
            foreach ($property in $Element.EnumerateObject()) {
                $result[$property.Name] = ConvertFrom-ParityJsonElement $property.Value
            }
            return $result
        }
        'Array' {
            $items = [System.Collections.Generic.List[object]]::new()
            foreach ($item in $Element.EnumerateArray()) { $items.Add((ConvertFrom-ParityJsonElement $item)) }
            return , $items.ToArray()
        }
        'Number' { return [decimal]::Parse($Element.GetRawText(), [System.Globalization.NumberStyles]::Float, $script:Invariant) }
        'String' { return $Element.GetString() }
        'True' { return $true }
        'False' { return $false }
        default { return $null }
    }
}

function ConvertFrom-ParityJson([string]$Json) {
    $document = [System.Text.Json.JsonDocument]::Parse($Json)
    try { return , (ConvertFrom-ParityJsonElement $document.RootElement) }
    finally { $document.Dispose() }
}

function New-ParityReport {
    , [System.Collections.Generic.SortedDictionary[string, object]]::new([System.StringComparer]::Ordinal)
}

function Add-ParityFigure($Report, [string]$Key, $Value, [string]$Side = 'report') {
    if ($Report.ContainsKey($Key)) { throw "Duplicate figure key $Key on the $Side side" }
    $Report[$Key] = $Value
}

function Get-ParitySum($Items, [scriptblock]$Selector) {
    $total = [decimal]0
    foreach ($item in @($Items)) {
        if ($null -eq $item) { continue }
        $value = & $Selector $item
        if ($null -ne $value) { $total += [decimal]$value }
    }
    $total
}

function Get-ParityMonthKeys([string]$First, [string]$Last) {
    $cursor = [datetime]::ParseExact($First, 'yyyy-MM', $script:Invariant)
    $end = [datetime]::ParseExact($Last, 'yyyy-MM', $script:Invariant)
    while ($cursor -le $end) {
        $cursor.ToString('yyyy-MM', $script:Invariant)
        $cursor = $cursor.AddMonths(1)
    }
}

function Add-ParitySeries($Report, [string]$Prefix, $Values, [string]$Side) {
    for ($index = 0; $index -lt @($Values).Count; $index++) {
        Add-ParityFigure $Report ('{0}/{1:00}' -f $Prefix, ($index + 1)) @($Values)[$index] $Side
    }
}

function Add-ParityScalarsAndSeries($Report, [string]$Prefix, $Object, [string[]]$Skip, [string]$Side) {
    foreach ($name in $Object.Keys) {
        if ($Skip -contains $name) { continue }
        $value = $Object[$name]
        if ($value -is [array]) { Add-ParitySeries $Report "$Prefix/$name" $value $Side }
        else { Add-ParityFigure $Report "$Prefix/$name" $value $Side }
    }
}

function Add-ParityMonthFigures($Report, [string]$Month, [scriptblock]$Fetch, $Banks, $Movements, [string]$Side) {
    $year = [int]$Month.Substring(0, 4)
    $monthNumber = [int]$Month.Substring(5, 2)
    $route = "$year/$monthNumber"

    $balances = & $Fetch "banks/month/$route/balances"
    foreach ($balance in $balances) { Add-ParityFigure $Report "$Month/banks/$($balance.bank)/balance" $balance.balance $Side }

    $expenses = & $Fetch "expenses/month/$route"
    $bankTotalsSum = [decimal]0
    $roundUpTotalsSum = [decimal]0
    foreach ($bank in $Banks) {
        $roundUp = Get-ParitySum ($expenses | Where-Object { $_.paymentSourceBankId -eq $bank.id }) { param($e) $e.roundUpAmount }
        Add-ParityFigure $Report "$Month/banks/$($bank.name)/roundUpTotal" $roundUp $Side
        $roundUpTotalsSum += $roundUp
        $match = @($balances | Where-Object { $_.bank -eq $bank.name } | Select-Object -First 1)
        if ($match.Count -gt 0) { $bankTotalsSum += [decimal]$match[0].balance }
    }

    $statements = & $Fetch "card-statements/$route"
    foreach ($statement in $statements) {
        $prefix = "$Month/cards/$($statement.creditCardName)"
        Add-ParityFigure $Report "$prefix/outstandingTotal" $statement.outstandingTotal $Side
        Add-ParityFigure $Report "$prefix/accumulatedOutstandingTotal" $statement.accumulatedOutstandingTotal $Side
        Add-ParityFigure $Report "$prefix/isPaid" $statement.isPaid $Side
        Add-ParityFigure $Report "$prefix/warning" $statement.warning $Side
    }

    $unpaid = & $Fetch "expenses/month/$route/unpaid-card-charges"
    Add-ParityFigure $Report "$Month/cards/unpaidChargesTotal" (Get-ParitySum $unpaid { param($e) $e.value }) $Side

    $categoryTotals = & $Fetch "expenses/month/$route/category-totals"
    foreach ($category in $categoryTotals) { Add-ParityFigure $Report "$Month/categories/$($category.category)/total" $category.totalValue $Side }

    $incomes = & $Fetch "incomes/month/$route"
    $bySource = [ordered]@{}
    foreach ($income in $incomes) {
        if (-not $bySource.Contains($income.incomeSourceName)) {
            $bySource[$income.incomeSourceName] = [ordered]@{ net = [decimal]0; gross = $null }
        }
        $entry = $bySource[$income.incomeSourceName]
        $entry.net += [decimal]$income.netValue
        if ($null -ne $income.grossValue) {
            $grossSoFar = if ($null -eq $entry.gross) { [decimal]0 } else { $entry.gross }
            $entry.gross = $grossSoFar + [decimal]$income.grossValue
        }
    }
    foreach ($source in $bySource.Keys) {
        Add-ParityFigure $Report "$Month/incomes/$source/net" $bySource[$source].net $Side
        Add-ParityFigure $Report "$Month/incomes/$source/gross" $bySource[$source].gross $Side
    }
    Add-ParityFigure $Report "$Month/incomes/totalIncoming" (Get-ParitySum $bySource.Values { param($e) $e.net }) $Side

    $tithe = & $Fetch "tithe/month/$route"
    Add-ParityFigure $Report "$Month/tithe/calculatedTithe" $tithe.calculatedTithe $Side
    Add-ParityFigure $Report "$Month/tithe/titheBalance" $tithe.titheBalance $Side
    if ($null -eq $tithe.carryForward) { Add-ParityFigure $Report "$Month/tithe/carryForward" $null $Side }
    else { Add-ParityScalarsAndSeries $Report "$Month/tithe/carryForward" $tithe.carryForward @() $Side }

    $suggestions = & $Fetch "investment-snapshots/$route/suggestions"
    foreach ($suggestion in $suggestions.suggestions) {
        $prefix = "$Month/snapshots/$($suggestion.accountName)"
        Add-ParityFigure $Report "$prefix/suggestedValue" $suggestion.suggestedValue $Side
        Add-ParityFigure $Report "$prefix/currentValue" $suggestion.currentValue $Side
        Add-ParityFigure $Report "$prefix/source" $suggestion.sourceDescription $Side
    }
    foreach ($skipped in $suggestions.notUpdated) {
        Add-ParityFigure $Report "$Month/snapshots/$($skipped.accountName)/skippedReason" $skipped.reason $Side
    }

    Add-ParityFigure $Report "$Month/monthly/bankTotalsSum" $bankTotalsSum $Side
    Add-ParityFigure $Report "$Month/monthly/roundUpTotalsSum" $roundUpTotalsSum $Side
    Add-ParityFigure $Report "$Month/monthly/cardOutstandingTotal" (Get-ParitySum $statements { param($s) $s.outstandingTotal }) $Side
    Add-ParityFigure $Report "$Month/monthly/categoryTotalsSum" (Get-ParitySum $categoryTotals { param($c) $c.totalValue }) $Side

    $monthEnd = [datetime]::ParseExact($Month, 'yyyy-MM', $script:Invariant).AddMonths(1)
    $potsTotal = [decimal]0
    foreach ($bucketName in @($Movements | ForEach-Object { $_.bucketName } | Sort-Object -Unique)) {
        $balance = Get-ParitySum ($Movements | Where-Object { $_.bucketName -eq $bucketName -and [datetime]::Parse($_.date, $script:Invariant) -lt $monthEnd }) { param($m) $m.amount }
        Add-ParityFigure $Report "$Month/pots/$bucketName/balance" $balance $Side
        $potsTotal += $balance
    }
    Add-ParityFigure $Report "$Month/pots/total" $potsTotal $Side
}

function Add-ParityYearFigures($Report, [int]$Year, [scriptblock]$Fetch, [string]$Side) {
    $annual = & $Fetch "annual-summary/$Year/category-totals"
    foreach ($category in $annual.categoryTotals) {
        $prefix = "$Year/annual/categories/$($category.category)"
        Add-ParityFigure $Report "$prefix/annualTotal" $category.annualTotal $Side
        Add-ParityFigure $Report "$prefix/average" $category.average $Side
        Add-ParitySeries $Report "$prefix/monthly" $category.monthlyTotals $Side
    }
    Add-ParityScalarsAndSeries $Report "$Year/annual/income" $annual.incomeSummary @() $Side
    Add-ParityScalarsAndSeries $Report "$Year/annual/totals" $annual @('categoryTotals', 'incomeSummary') $Side

    foreach ($row in (& $Fetch "annual-summary/$Year/historic-summary-averages")) {
        foreach ($average in $row.annualAverages) {
            Add-ParityFigure $Report "$Year/averages/$($row.year)/$($average.category)" $average.value $Side
        }
    }

    $investment = & $Fetch "annual-summary/$Year/investment-annual-result"
    foreach ($account in $investment.accounts) {
        $prefix = "$Year/investment/$($account.account)"
        Add-ParityFigure $Report "$prefix/isLiability" $account.isLiability $Side
        Add-ParitySeries $Report "$prefix/value" $account.monthlyValues $Side
        Add-ParitySeries $Report "$prefix/diff" $account.monthlyDiffs $Side
    }
    $net = $investment.netPosition
    Add-ParitySeries $Report "$Year/investment/net/value" $net.monthlyValues $Side
    Add-ParitySeries $Report "$Year/investment/net/diff" $net.monthlyDiffs $Side
    Add-ParityScalarsAndSeries $Report "$Year/investment/net" $net @('monthlyValues', 'monthlyDiffs') $Side
}

function Get-ParityReport {
    param(
        [Parameter(Mandatory)][scriptblock]$Fetch,
        [Parameter(Mandatory)][string]$First,
        [Parameter(Mandatory)][string]$Last,
        [string]$Side = 'report'
    )
    $report = New-ParityReport
    $banks = & $Fetch 'banks'
    $movements = & $Fetch 'reserve/movements'
    foreach ($month in Get-ParityMonthKeys $First $Last) {
        Add-ParityMonthFigures $report $month $Fetch $banks $movements $Side
    }

    foreach ($balance in (& $Fetch 'reserve/balances')) {
        Add-ParityFigure $report "current/pots/$($balance.bucketName)/balance" $balance.balance $Side
    }
    $mae = & $Fetch 'controle-mae/entries/totals'
    Add-ParityFigure $report 'current/mae/totalBrl' $mae.totalBrlValue $Side
    Add-ParityFigure $report 'current/mae/totalGbp' $mae.totalGbpValue $Side

    for ($year = [int]$First.Substring(0, 4); $year -le [int]$Last.Substring(0, 4); $year++) {
        Add-ParityYearFigures $report $year $Fetch $Side
    }
    , $report
}

function Format-ParityValue($Value) {
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { return $Value.ToString().ToLowerInvariant() }
    if ($Value -is [decimal]) { return $Value.ToString($script:Invariant) }
    [string]$Value
}

function ConvertTo-ParityJsonValue($Value) {
    if ($null -eq $Value -or $Value -is [bool] -or $Value -is [decimal]) { return Format-ParityValue $Value }
    [System.Text.Json.JsonSerializer]::Serialize([string]$Value, $script:JsonWriteOptions)
}

function ConvertTo-ParityReportJson($Report) {
    $lines = foreach ($key in $Report.Keys) {
        '  {0}: {1}' -f [System.Text.Json.JsonSerializer]::Serialize($key, $script:JsonWriteOptions), (ConvertTo-ParityJsonValue $Report[$key])
    }
    "{`n" + ($lines -join ",`n") + "`n}`n"
}

function Read-ParityReport([string]$Path) {
    $parsed = ConvertFrom-ParityJson (Get-Content -LiteralPath $Path -Raw)
    $report = New-ParityReport
    foreach ($key in $parsed.Keys) { $report[$key] = $parsed[$key] }
    , $report
}

function Read-ParityFigureMap([string]$Path) {
    $parsed = ConvertFrom-ParityJson (Get-Content -LiteralPath $Path -Raw)
    , @($parsed.mappings)
}

function ConvertTo-ParityPatternRegex([string]$Pattern) {
    $segments = foreach ($segment in $Pattern.Split('/')) {
        switch ($segment) {
            '*' { '([^/]+)' }
            '{month}' { '(\d{4}-\d{2})' }
            default { [regex]::Escape($segment) }
        }
    }
    '^' + ($segments -join '/') + '$'
}

function Resolve-ParityBeforeKey([string]$AfterKey, $Mappings) {
    foreach ($mapping in @($Mappings)) {
        $match = [regex]::Match($AfterKey, (ConvertTo-ParityPatternRegex $mapping.after))
        if (-not $match.Success) { continue }
        $captureIndex = 0
        $segments = foreach ($segment in $mapping.before.Split('/')) {
            if ($segment -eq '*' -or $segment -eq '{month}') {
                $captureIndex++
                $match.Groups[$captureIndex].Value
            }
            else { $segment }
        }
        return [pscustomobject]@{ Key = ($segments -join '/'); Negate = [bool]$mapping['negate'] }
    }
    [pscustomobject]@{ Key = $AfterKey; Negate = $false }
}

function Test-ParityValuesEqual($Before, $After, [bool]$Negate) {
    if ($null -eq $Before -or $null -eq $After) { return ($null -eq $Before -and $null -eq $After) }
    if ($Before -is [decimal] -and $After -is [decimal]) {
        $expected = if ($Negate) { -$Before } else { $Before }
        return $After -eq $expected
    }
    if ($Before.GetType() -ne $After.GetType()) { return $false }
    [string]::Equals([string]$Before, [string]$After, [System.StringComparison]::Ordinal)
}

function Compare-ParityReport($Before, $After, $Mappings = @()) {
    $differences = [System.Collections.Generic.List[object]]::new()
    $reached = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($afterKey in $After.Keys) {
        $resolved = Resolve-ParityBeforeKey $afterKey $Mappings
        if (-not $Before.ContainsKey($resolved.Key)) {
            $differences.Add([pscustomobject]@{ Key = $afterKey; Before = '(missing)'; After = Format-ParityValue $After[$afterKey] })
            continue
        }
        [void]$reached.Add($resolved.Key)
        if (-not (Test-ParityValuesEqual $Before[$resolved.Key] $After[$afterKey] $resolved.Negate)) {
            $differences.Add([pscustomobject]@{ Key = $afterKey; Before = Format-ParityValue $Before[$resolved.Key]; After = Format-ParityValue $After[$afterKey] })
        }
    }
    foreach ($beforeKey in $Before.Keys) {
        if (-not $reached.Contains($beforeKey)) {
            $differences.Add([pscustomobject]@{ Key = $beforeKey; Before = Format-ParityValue $Before[$beforeKey]; After = '(missing)' })
        }
    }
    , $differences.ToArray()
}

function Format-ParityDifference($Difference) {
    '{0} | {1} | {2}' -f ($Difference.Key.Split('/') -join ' / '), $Difference.Before, $Difference.After
}

function Get-ParityMonthCount($Report) {
    @($Report.Keys | Where-Object { $_ -match '^\d{4}-\d{2}/' } | ForEach-Object { $_.Substring(0, 7) } | Sort-Object -Unique).Count
}

function Format-ParitySummary([int]$DifferenceCount, [int]$FigureCount, [int]$MonthCount) {
    if ($DifferenceCount -eq 0) {
        return 'Parity OK: 0 differences in {0} figures across {1} months' -f $FigureCount.ToString('N0', $script:Invariant), $MonthCount
    }
    'Parity FAILED: {0} difference{1}' -f $DifferenceCount, $(if ($DifferenceCount -eq 1) { '' } else { 's' })
}

function Get-ParityMonthRange([string]$DataFile, [datetime]$Now) {
    $dateFields = @{
        Expenses           = @('Date', 'ChargeDate', 'InvoiceDate')
        Incomes            = @('Date')
        ReserveMovements   = @('Date')
        MaeLedgerEntries   = @('Date')
        Transfers          = @('Date')
        BalanceAdjustments = @('Date')
    }
    $months = [System.Collections.Generic.List[string]]::new()
    $document = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($DataFile))
    try {
        $root = $document.RootElement
        foreach ($collection in $dateFields.Keys) {
            $array = [System.Text.Json.JsonElement]::new()
            if (-not $root.TryGetProperty($collection, [ref]$array)) { continue }
            foreach ($record in $array.EnumerateArray()) {
                foreach ($field in $dateFields[$collection]) {
                    $value = [System.Text.Json.JsonElement]::new()
                    if ($record.TryGetProperty($field, [ref]$value) -and $value.ValueKind -eq 'String') {
                        $months.Add($value.GetString().Substring(0, 7))
                    }
                }
            }
        }
        foreach ($collection in @('InvestmentSnapshots', 'CardStatements')) {
            $array = [System.Text.Json.JsonElement]::new()
            if (-not $root.TryGetProperty($collection, [ref]$array)) { continue }
            foreach ($record in $array.EnumerateArray()) {
                $months.Add(('{0:0000}-{1:00}' -f $record.GetProperty('Year').GetInt32(), $record.GetProperty('Month').GetInt32()))
            }
        }
    }
    finally { $document.Dispose() }

    $currentMonth = $Now.ToString('yyyy-MM', $script:Invariant)
    if ($months.Count -eq 0) { return [pscustomobject]@{ First = $currentMonth; Last = $currentMonth; CurrentMonth = $currentMonth } }
    $sorted = $months | Sort-Object -Unique
    $lastRecord = @($sorted)[-1]
    [pscustomobject]@{
        First        = @($sorted)[0]
        Last         = if ([string]::CompareOrdinal($lastRecord, $currentMonth) -gt 0) { $lastRecord } else { $currentMonth }
        CurrentMonth = $currentMonth
    }
}

function Assert-ParityLiveCopy([string]$Path, [string]$RepoRoot) {
    $resolved = [System.IO.Path]::GetFullPath($Path, (Get-Location).ProviderPath)
    $liveFile = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot 'data/data-cashflow.json'))
    if ([string]::Equals($resolved, $liveFile, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to run on the live data file data/data-cashflow.json. Pass a copy.'
    }
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { throw "Live copy not found: $resolved" }
    $resolved
}

function Assert-ParityPorts([int[]]$Ports) {
    $listening = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() | ForEach-Object { $_.Port }
    foreach ($port in $Ports) {
        $isRepeated = @($Ports | Where-Object { $_ -eq $port }).Count -gt 1
        if ($port -eq 8080 -or $isRepeated -or $listening -contains $port) {
            throw "Port $port is in use (or is the live port 8080). Free it or pass -BeforePort/-AfterPort."
        }
    }
}

function Get-ParitySha256([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Assert-ParityHashUnchanged([string]$Path, [string]$ExpectedHash) {
    if ((Get-ParitySha256 $Path) -ne $ExpectedHash) {
        throw 'The input copy changed during the run; results discarded.'
    }
}

Export-ModuleMember -Function *-Parity*
