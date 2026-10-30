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

function Add-ParityBankFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch, $Banks) {
    $balanceByBank = @{}
    foreach ($balance in (& $Fetch "banks/month/$Route/balances")) {
        Add-ParityFigure $Report "$Month/banks/$($balance.bank)/balance" $balance.balance
        $balanceByBank[$balance.bank] = $balance.balance
    }

    $roundUpByBankId = @{}
    foreach ($expense in (& $Fetch "expenses/month/$Route")) {
        if ($null -eq $expense.paymentSourceBankId -or $null -eq $expense.roundUpAmount) { continue }
        $roundUpByBankId[$expense.paymentSourceBankId] = [decimal]$roundUpByBankId[$expense.paymentSourceBankId] + $expense.roundUpAmount
    }

    $totals = [pscustomobject]@{ BankTotalsSum = [decimal]0; RoundUpTotalsSum = [decimal]0 }
    foreach ($bank in $Banks) {
        $roundUp = [decimal]$roundUpByBankId[$bank.id]
        Add-ParityFigure $Report "$Month/banks/$($bank.name)/roundUpTotal" $roundUp
        $totals.RoundUpTotalsSum += $roundUp
        $totals.BankTotalsSum += [decimal]$balanceByBank[$bank.name]
    }
    $totals
}

function Add-ParityStatementFigures($Report, [string]$Prefix, $Statement) {
    Add-ParityFigure $Report "$Prefix/outstandingTotal" $Statement.outstandingTotal
    Add-ParityFigure $Report "$Prefix/accumulatedOutstandingTotal" $Statement.accumulatedOutstandingTotal
    Add-ParityFigure $Report "$Prefix/isPaid" $Statement.isPaid
    Add-ParityFigure $Report "$Prefix/warning" ($Statement.PSObject.Properties['warning'])?.Value
}

function Add-ParityCardFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch) {
    $statements = & $Fetch "card-statements/$Route"
    foreach ($statement in $statements) {
        Add-ParityStatementFigures $Report "$Month/cards/$($statement.creditCardName)" $statement
    }
    $unpaid = & $Fetch "expenses/month/$Route/unpaid-card-charges"
    Add-ParityFigure $Report "$Month/cards/unpaidChargesTotal" (Get-ParitySum $unpaid { param($e) $e.value })
    Get-ParitySum $statements { param($s) $s.outstandingTotal }
}

function Add-ParityMissingCardStatementFigures($Report, [string[]]$Months, $Accounts) {
    foreach ($card in @($Accounts | Where-Object { $_['templateName'] -eq 'CreditCard' -and $_['isActive'] -eq $true })) {
        $accumulated = [decimal]0
        foreach ($month in $Months) {
            $key = "$month/cards/$($card.name)/accumulatedOutstandingTotal"
            if ($Report.ContainsKey($key)) { $accumulated = $Report[$key]; break }
        }
        foreach ($month in $Months) {
            $prefix = "$month/cards/$($card.name)"
            if ($Report.ContainsKey("$prefix/outstandingTotal")) { continue }
            Add-ParityStatementFigures $Report $prefix ([pscustomobject]@{ outstandingTotal = [decimal]0; accumulatedOutstandingTotal = $accumulated; isPaid = $false })
        }
    }
}

function Add-ParityCategoryFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch) {
    $categoryTotals = & $Fetch "expenses/month/$Route/category-totals"
    foreach ($category in $categoryTotals) { Add-ParityFigure $Report "$Month/categories/$($category.category)/total" $category.totalValue }
    Get-ParitySum $categoryTotals { param($c) $c.totalValue }
}

function Add-ParityIncomeFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch) {
    $bySource = [ordered]@{}
    foreach ($income in (& $Fetch "incomes/month/$Route")) {
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
}

function Add-ParityTitheFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch) {
    $tithe = & $Fetch "tithe/month/$Route"
    Add-ParityFigure $Report "$Month/tithe/calculatedTithe" $tithe.calculatedTithe
    Add-ParityFigure $Report "$Month/tithe/titheBalance" $tithe.titheBalance
    Add-ParityScalarsAndSeries $Report "$Month/tithe/carryForward" $tithe.carryForward
}

function Add-ParitySnapshotFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch) {
    $suggestions = & $Fetch "investment-snapshots/$Route/suggestions"
    foreach ($suggestion in $suggestions.suggestions) {
        $prefix = "$Month/snapshots/$($suggestion.accountName)"
        Add-ParityFigure $Report "$prefix/suggestedValue" $suggestion.suggestedValue
        Add-ParityFigure $Report "$prefix/currentValue" $suggestion.currentValue
        Add-ParityFigure $Report "$prefix/source" $suggestion.sourceDescription
    }
    foreach ($skipped in $suggestions.notUpdated) {
        Add-ParityFigure $Report "$Month/snapshots/$($skipped.accountName)/skippedReason" $skipped.reason
    }
}

function Add-ParityPotFigures($Report, [string]$Month, $PotBalances) {
    foreach ($bucketName in $PotBalances.Keys) {
        Add-ParityFigure $Report "$Month/pots/$bucketName/balance" $PotBalances[$bucketName]
    }
    Add-ParityFigure $Report "$Month/pots/total" (Get-ParitySum $PotBalances.Values { param($b) $b })
}

function Add-ParitySummaryFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch, [bool]$IsCurrentMonth) {
    $summary = & $Fetch "accounts/groups/default/summary/$Route"
    foreach ($member in $summary.members) {
        if ($null -ne $member['statement']) {
            $prefix = "$Month/summary/cardMembers/$($member.name)"
            Add-ParityFigure $Report "$prefix/outstandingTotal" $member.statement.outstandingTotal
            Add-ParityFigure $Report "$prefix/isPaid" $member.statement.isPaid
            if ($IsCurrentMonth) { Add-ParityFigure $Report "$prefix/balance" $member.balance }
            continue
        }
        Add-ParityFigure $Report "$Month/summary/members/$($member.name)/balance" $member.balance
        Add-ParityFigure $Report "$Month/summary/members/$($member.name)/roundUpTotal" $member.roundUpTotal
    }
    foreach ($total in $summary.totals) {
        Add-ParityFigure $Report "$Month/summary/totals/$($total.currency)/balance" $total.balance
        Add-ParityFigure $Report "$Month/summary/totals/$($total.currency)/roundUpTotal" $total.roundUpTotal
        Add-ParityFigure $Report "$Month/summary/totals/$($total.currency)/cardOutstandingTotal" $total.cardOutstandingTotal
    }
    Add-ParityFigure $Report "$Month/summary/categoryTotalsSum" $summary.categoryTotalsSum
    foreach ($income in $summary.incomeTotals) {
        Add-ParityFigure $Report "$Month/summary/incomes/$($income.name)/net" $income.netValue
        Add-ParityFigure $Report "$Month/summary/incomes/$($income.name)/gross" $income.grossValue
    }
    Add-ParityFigure $Report "$Month/summary/totalIncoming" $summary.totalIncoming
}

function Add-ParityOtherAccountsFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch) {
    $members = & $Fetch "accounts/groups/other/$Route"
    $potsAccounts = @($members | Where-Object { $_['templateName'] -eq 'Pots' })
    Add-ParityFigure $Report "$Month/otherAccounts/potsTotal" (Get-ParitySum $potsAccounts { param($a) $a.balance })
}

function Add-ParityAccountFigures($Report, [string]$Month, [string]$Route, [scriptblock]$Fetch, $Accounts, [bool]$IsCurrentMonth) {
    foreach ($account in @($Accounts | Where-Object { $_['templateName'] -in 'Bank', 'CreditCard' })) {
        $isCard = $account['templateName'] -eq 'CreditCard'
        $isActive = $account['isActive'] -eq $true
        if ($isCard -and -not ($IsCurrentMonth -or $isActive)) { continue }
        $detail = & $Fetch "accounts/$($account.id)/$Route"
        if (-not $isCard) {
            Add-ParityFigure $Report "$Month/account/$($account.name)/balance" $detail.balance
            continue
        }
        if ($IsCurrentMonth) { Add-ParityFigure $Report "$Month/cardAccount/$($account.name)/balance" $detail.balance }
        if ($isActive) { Add-ParityFigure $Report "$Month/cardAccount/$($account.name)/totalOwed" $detail.totalOwed }
    }
}

function Add-ParityMonthFigures($Report, [string]$Month, [scriptblock]$Fetch, $Banks, $PotBalances, [bool]$IncludeSummary, $Accounts, [string]$CurrentMonth) {
    $route = '{0}/{1}' -f [int]$Month.Substring(0, 4), [int]$Month.Substring(5, 2)
    $bankTotals = Add-ParityBankFigures $Report $Month $route $Fetch $Banks
    $cardOutstandingTotal = Add-ParityCardFigures $Report $Month $route $Fetch
    $categoryTotalsSum = Add-ParityCategoryFigures $Report $Month $route $Fetch
    Add-ParityIncomeFigures $Report $Month $route $Fetch
    Add-ParityTitheFigures $Report $Month $route $Fetch
    Add-ParitySnapshotFigures $Report $Month $route $Fetch
    Add-ParityFigure $Report "$Month/monthly/bankTotalsSum" $bankTotals.BankTotalsSum
    Add-ParityFigure $Report "$Month/monthly/roundUpTotalsSum" $bankTotals.RoundUpTotalsSum
    Add-ParityFigure $Report "$Month/monthly/cardOutstandingTotal" $cardOutstandingTotal
    Add-ParityFigure $Report "$Month/monthly/categoryTotalsSum" $categoryTotalsSum
    Add-ParityPotFigures $Report $Month $PotBalances
    if ($IncludeSummary) {
        Add-ParitySummaryFigures $Report $Month $route $Fetch ($Month -eq $CurrentMonth)
        Add-ParityOtherAccountsFigures $Report $Month $route $Fetch
    }
    if ($null -ne $Accounts) { Add-ParityAccountFigures $Report $Month $route $Fetch $Accounts ($Month -eq $CurrentMonth) }
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


function Get-ParityPotBalancesByMonth($Movements, [string[]]$Months) {
    $ordered = @(@(foreach ($movement in $Movements) {
                [pscustomobject]@{ Bucket = $movement.bucketName; Date = [datetime]::Parse($movement.date, $script:Invariant); Amount = $movement.amount }
            }) | Sort-Object Date)
    $running = [ordered]@{}
    foreach ($bucketName in @($ordered | ForEach-Object Bucket | Sort-Object -Unique)) { $running[$bucketName] = [decimal]0 }
    $byMonth = @{}
    $next = 0
    foreach ($month in $Months) {
        $monthEnd = [datetime]::ParseExact($month, 'yyyy-MM', $script:Invariant).AddMonths(1)
        while ($next -lt $ordered.Count -and $ordered[$next].Date -lt $monthEnd) {
            $running[$ordered[$next].Bucket] += $ordered[$next].Amount
            $next++
        }
        $snapshot = [ordered]@{}
        foreach ($bucketName in $running.Keys) { $snapshot[$bucketName] = $running[$bucketName] }
        $byMonth[$month] = $snapshot
    }
    $byMonth
}

function Get-ParityReport {
    param(
        [Parameter(Mandatory)][scriptblock]$Fetch,
        [Parameter(Mandatory)][string]$First,
        [Parameter(Mandatory)][string]$Last,
        [string]$Side = 'report',
        [string]$CurrentMonth = '',
        [switch]$IncludeSummary,
        [switch]$IncludeAccounts,
        [switch]$FillMissingStatements
    )
    try {
        $report = New-ParityReport
        $months = @(Get-ParityMonthKeys $First $Last)
        $banks = & $Fetch 'banks'
        $potBalancesByMonth = Get-ParityPotBalancesByMonth (& $Fetch 'reserve/movements') $months
        $accounts = if ($IncludeAccounts -or $FillMissingStatements) { @(& $Fetch 'accounts') } else { $null }
        $figureAccounts = if ($IncludeAccounts) { $accounts } else { $null }
        foreach ($month in $months) {
            Add-ParityMonthFigures $report $month $Fetch $banks $potBalancesByMonth[$month] $IncludeSummary.IsPresent $figureAccounts $CurrentMonth
        }
        if ($FillMissingStatements) { Add-ParityMissingCardStatementFigures $report $months $accounts }

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
    $placeholderCount = { param($pattern) @($pattern.Split('/') | Where-Object { $_ -eq '*' -or $_ -eq '{month}' }).Count }
    if ($Mapping -isnot [System.Collections.IDictionary] -or $Mapping['after'] -isnot [string] -or $Mapping['before'] -isnot [string] -or
        (& $placeholderCount $Mapping['after']) -ne (& $placeholderCount $Mapping['before'])) {
        throw "Invalid figure-map entry: each mapping needs 'after' and 'before' patterns with the same placeholders ($($Mapping | ConvertTo-Json -Compress))"
    }
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

function Resolve-ParityLiveCopy([string]$Path, [string[]]$RepoRoots) {
    if (-not $Path) {
        foreach ($repoRoot in $RepoRoots) {
            $liveFile = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'data/data-cashflow.json'))
            if (Test-Path -LiteralPath $liveFile -PathType Leaf) { return $liveFile }
        }
        throw 'data/data-cashflow.json not found. Download it from Google Drive into data/, or pass -LiveCopy.'
    }
    $resolved = [System.IO.Path]::GetFullPath($Path, (Get-Location).ProviderPath)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { throw "Live copy not found: $resolved" }
    $resolved
}

function Copy-ParitySource([string]$Source, [string]$Folder) {
    $copy = Join-Path $Folder 'source.json'
    Copy-Item -LiteralPath $Source -Destination $copy -Force
    $copy
}

function Format-ParitySource([string]$Source) {
    $document = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($Source))
    try {
        $versionProperty = $document.RootElement.EnumerateObject() | Where-Object Name -eq 'Version' | Select-Object -First 1
        $version = if ($versionProperty) { $versionProperty.Value.ToString() } else { 'none' }
    }
    finally { $document.Dispose() }
    $savedAt = (Get-Item -LiteralPath $Source).LastWriteTime.ToString('yyyy-MM-dd HH:mm')
    "Source: $Source (version $version, SHA-256 $((Get-ParitySha256 $Source).Substring(0, 8)), saved $savedAt)"
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
