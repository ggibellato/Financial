Set-StrictMode -Version Latest

$script:Invariant = [System.Globalization.CultureInfo]::InvariantCulture
$script:JsonWriteOptions = [System.Text.Json.JsonSerializerOptions]::new()
$script:JsonWriteOptions.Encoder = [System.Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping

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

function Add-ParityFigure($Report, [string]$Key, $Value) {
    if ($Report.ContainsKey($Key)) { throw "Duplicate figure key $Key" }
    $Report[$Key] = $Value
}

function Get-ParitySum($Items, [scriptblock]$Selector) {
    $total = [decimal]0
    foreach ($item in $Items) {
        $value = & $Selector $item
        if ($null -eq $value) { throw 'A summed figure is null.' }
        $total += $value
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

function Add-ParitySeries($Report, [string]$Prefix, $Values) {
    $series = @($Values)
    for ($index = 0; $index -lt $series.Count; $index++) {
        Add-ParityFigure $Report ('{0}/{1:00}' -f $Prefix, ($index + 1)) $series[$index]
    }
}

function Add-ParityScalarsAndSeries($Report, [string]$Prefix, $Object, [string[]]$Skip = @()) {
    if ($null -eq $Object) {
        Add-ParityFigure $Report $Prefix $null
        return
    }
    foreach ($name in $Object.Keys) {
        if ($Skip -contains $name) { continue }
        $value = $Object[$name]
        if ($value -is [array]) { Add-ParitySeries $Report "$Prefix/$name" $value }
        else { Add-ParityFigure $Report "$Prefix/$name" $value }
    }
}

function Add-ParityMonthFigures($Report, [string]$Month, [scriptblock]$Fetch, $Banks, $PotBalances) {
    $route = '{0}/{1}' -f [int]$Month.Substring(0, 4), [int]$Month.Substring(5, 2)

    $balances = & $Fetch "banks/month/$route/balances"
    $balanceByBank = @{}
    foreach ($balance in $balances) {
        Add-ParityFigure $Report "$Month/banks/$($balance.bank)/balance" $balance.balance
        $balanceByBank[$balance.bank] = $balance.balance
    }

    $roundUpByBankId = @{}
    foreach ($expense in (& $Fetch "expenses/month/$route")) {
        if ($null -eq $expense.paymentSourceBankId -or $null -eq $expense.roundUpAmount) { continue }
        $roundUpByBankId[$expense.paymentSourceBankId] = [decimal]$roundUpByBankId[$expense.paymentSourceBankId] + $expense.roundUpAmount
    }
    $bankTotalsSum = [decimal]0
    $roundUpTotalsSum = [decimal]0
    foreach ($bank in $Banks) {
        $roundUp = [decimal]$roundUpByBankId[$bank.id]
        Add-ParityFigure $Report "$Month/banks/$($bank.name)/roundUpTotal" $roundUp
        $roundUpTotalsSum += $roundUp
        $bankTotalsSum += [decimal]$balanceByBank[$bank.name]
    }

    $statements = & $Fetch "card-statements/$route"
    foreach ($statement in $statements) {
        $prefix = "$Month/cards/$($statement.creditCardName)"
        Add-ParityFigure $Report "$prefix/outstandingTotal" $statement.outstandingTotal
        Add-ParityFigure $Report "$prefix/accumulatedOutstandingTotal" $statement.accumulatedOutstandingTotal
        Add-ParityFigure $Report "$prefix/isPaid" $statement.isPaid
        Add-ParityFigure $Report "$prefix/warning" $statement.warning
    }

    $unpaid = & $Fetch "expenses/month/$route/unpaid-card-charges"
    Add-ParityFigure $Report "$Month/cards/unpaidChargesTotal" (Get-ParitySum $unpaid { param($e) $e.value })

    $categoryTotals = & $Fetch "expenses/month/$route/category-totals"
    foreach ($category in $categoryTotals) { Add-ParityFigure $Report "$Month/categories/$($category.category)/total" $category.totalValue }

    $bySource = [ordered]@{}
    foreach ($income in (& $Fetch "incomes/month/$route")) {
        if (-not $bySource.Contains($income.incomeSourceName)) {
            $bySource[$income.incomeSourceName] = [ordered]@{ net = [decimal]0; gross = $null }
        }
        $entry = $bySource[$income.incomeSourceName]
        $entry.net += $income.netValue
        if ($null -ne $income.grossValue) { $entry.gross += $income.grossValue }
    }
    foreach ($source in $bySource.Keys) {
        Add-ParityFigure $Report "$Month/incomes/$source/net" $bySource[$source].net
        Add-ParityFigure $Report "$Month/incomes/$source/gross" $bySource[$source].gross
    }
    Add-ParityFigure $Report "$Month/incomes/totalIncoming" (Get-ParitySum $bySource.Values { param($e) $e.net })

    $tithe = & $Fetch "tithe/month/$route"
    Add-ParityFigure $Report "$Month/tithe/calculatedTithe" $tithe.calculatedTithe
    Add-ParityFigure $Report "$Month/tithe/titheBalance" $tithe.titheBalance
    Add-ParityScalarsAndSeries $Report "$Month/tithe/carryForward" $tithe.carryForward

    $suggestions = & $Fetch "investment-snapshots/$route/suggestions"
    foreach ($suggestion in $suggestions.suggestions) {
        $prefix = "$Month/snapshots/$($suggestion.accountName)"
        Add-ParityFigure $Report "$prefix/suggestedValue" $suggestion.suggestedValue
        Add-ParityFigure $Report "$prefix/currentValue" $suggestion.currentValue
        Add-ParityFigure $Report "$prefix/source" $suggestion.sourceDescription
    }
    foreach ($skipped in $suggestions.notUpdated) {
        Add-ParityFigure $Report "$Month/snapshots/$($skipped.accountName)/skippedReason" $skipped.reason
    }

    Add-ParityFigure $Report "$Month/monthly/bankTotalsSum" $bankTotalsSum
    Add-ParityFigure $Report "$Month/monthly/roundUpTotalsSum" $roundUpTotalsSum
    Add-ParityFigure $Report "$Month/monthly/cardOutstandingTotal" (Get-ParitySum $statements { param($s) $s.outstandingTotal })
    Add-ParityFigure $Report "$Month/monthly/categoryTotalsSum" (Get-ParitySum $categoryTotals { param($c) $c.totalValue })

    foreach ($bucketName in $PotBalances.Keys) {
        Add-ParityFigure $Report "$Month/pots/$bucketName/balance" $PotBalances[$bucketName]
    }
    Add-ParityFigure $Report "$Month/pots/total" (Get-ParitySum $PotBalances.Values { param($b) $b })
}

function Add-ParityYearFigures($Report, [int]$Year, [scriptblock]$Fetch) {
    $annual = & $Fetch "annual-summary/$Year/category-totals"
    foreach ($category in $annual.categoryTotals) {
        $prefix = "$Year/annual/categories/$($category.category)"
        Add-ParityFigure $Report "$prefix/annualTotal" $category.annualTotal
        Add-ParityFigure $Report "$prefix/average" $category.average
        Add-ParitySeries $Report "$prefix/monthly" $category.monthlyTotals
    }
    Add-ParityScalarsAndSeries $Report "$Year/annual/income" $annual.incomeSummary
    Add-ParityScalarsAndSeries $Report "$Year/annual/totals" $annual @('categoryTotals', 'incomeSummary')

    foreach ($row in (& $Fetch "annual-summary/$Year/historic-summary-averages")) {
        foreach ($average in $row.annualAverages) {
            Add-ParityFigure $Report "$Year/averages/$($row.year)/$($average.category)" $average.value
        }
    }

    $investment = & $Fetch "annual-summary/$Year/investment-annual-result"
    foreach ($account in $investment.accounts) {
        $prefix = "$Year/investment/$($account.account)"
        Add-ParityFigure $Report "$prefix/isLiability" $account.isLiability
        Add-ParitySeries $Report "$prefix/value" $account.monthlyValues
        Add-ParitySeries $Report "$prefix/diff" $account.monthlyDiffs
    }
    $net = $investment.netPosition
    Add-ParitySeries $Report "$Year/investment/net/value" $net.monthlyValues
    Add-ParitySeries $Report "$Year/investment/net/diff" $net.monthlyDiffs
    Add-ParityScalarsAndSeries $Report "$Year/investment/net" $net @('monthlyValues', 'monthlyDiffs')
}

function Get-ParityReport {
    param(
        [Parameter(Mandatory)][scriptblock]$Fetch,
        [Parameter(Mandatory)][string]$First,
        [Parameter(Mandatory)][string]$Last,
        [string]$Side = 'report'
    )
    try {
        $report = New-ParityReport
        $banks = & $Fetch 'banks'
        $movements = @(@(foreach ($movement in (& $Fetch 'reserve/movements')) {
                [pscustomobject]@{ Bucket = $movement.bucketName; Date = [datetime]::Parse($movement.date, $script:Invariant); Amount = $movement.amount }
            }) | Sort-Object Date)
        $potBalances = [ordered]@{}
        foreach ($bucketName in @($movements | ForEach-Object Bucket | Sort-Object -Unique)) { $potBalances[$bucketName] = [decimal]0 }
        $nextMovement = 0
        foreach ($month in Get-ParityMonthKeys $First $Last) {
            $monthEnd = [datetime]::ParseExact($month, 'yyyy-MM', $script:Invariant).AddMonths(1)
            while ($nextMovement -lt $movements.Count -and $movements[$nextMovement].Date -lt $monthEnd) {
                $potBalances[$movements[$nextMovement].Bucket] += $movements[$nextMovement].Amount
                $nextMovement++
            }
            Add-ParityMonthFigures $report $month $Fetch $banks $potBalances
        }

        foreach ($balance in (& $Fetch 'reserve/balances')) {
            Add-ParityFigure $report "current/pots/$($balance.bucketName)/balance" $balance.balance
        }
        $mae = & $Fetch 'controle-mae/entries/totals'
        Add-ParityFigure $report 'current/mae/totalBrl' $mae.totalBrlValue
        Add-ParityFigure $report 'current/mae/totalGbp' $mae.totalGbpValue

        for ($year = [int]$First.Substring(0, 4); $year -le [int]$Last.Substring(0, 4); $year++) {
            Add-ParityYearFigures $report $year $Fetch
        }
    }
    catch { throw "$($_.Exception.Message) on the $Side side" }
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

function ConvertTo-ParityMappingRule($Mapping) {
    $segments = foreach ($segment in $Mapping.after.Split('/')) {
        switch ($segment) {
            '*' { '([^/]+)' }
            '{month}' { '(\d{4}-\d{2})' }
            default { [regex]::Escape($segment) }
        }
    }
    [pscustomobject]@{
        Regex  = [regex]::new('^' + ($segments -join '/') + '$')
        Before = $Mapping.before.Split('/')
        Negate = [bool]$Mapping['negate']
    }
}

function Resolve-ParityBeforeKey([string]$AfterKey, $Rules) {
    foreach ($rule in $Rules) {
        $match = $rule.Regex.Match($AfterKey)
        if (-not $match.Success) { continue }
        $captureIndex = 0
        $segments = foreach ($segment in $rule.Before) {
            if ($segment -eq '*' -or $segment -eq '{month}') {
                $captureIndex++
                $match.Groups[$captureIndex].Value
            }
            else { $segment }
        }
        return [pscustomobject]@{ Key = ($segments -join '/'); Negate = $rule.Negate }
    }
    [pscustomobject]@{ Key = $AfterKey; Negate = $false }
}

function Test-ParityValuesEqual($Before, $After, [bool]$Negate) {
    if ($null -eq $Before -or $null -eq $After) { return ($null -eq $Before -and $null -eq $After) }
    if ($Before -is [decimal] -and $After -is [decimal]) {
        $expected = if ($Negate) { -$Before } else { $Before }
        return $After -eq $expected
    }
    $Before.GetType() -eq $After.GetType() -and $Before -ceq $After
}

function Compare-ParityReport($Before, $After, $Mappings = @()) {
    $rules = @($Mappings | ForEach-Object { ConvertTo-ParityMappingRule $_ })
    $differences = [System.Collections.Generic.List[object]]::new()
    $reached = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($afterKey in $After.Keys) {
        $resolved = Resolve-ParityBeforeKey $afterKey $rules
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
    $months = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($key in $Report.Keys) { if ($key -match '^\d{4}-\d{2}/') { [void]$months.Add($key.Substring(0, 7)) } }
    $months.Count
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
