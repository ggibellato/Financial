$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $here 'Parity.psm1') -Force
$entry = Join-Path $here 'Invoke-Parity.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "parity-test-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work | Out-Null

$failures = 0
$cases = 0

function Check([string]$name, [bool]$condition, $detail = '') {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: $detail"
    }
}

function New-Report([hashtable]$figures) {
    $report = New-ParityReport
    foreach ($key in $figures.Keys) { $report[$key] = $figures[$key] }
    , $report
}

function Get-Error([scriptblock]$action) {
    try { $null = & $action; return '' } catch { return $_.Exception.Message }
}

$figureFamilies = @('banks', 'cards', 'categories', 'incomes', 'tithe', 'snapshots', 'monthly', 'pots', 'summary', 'templateGroups', 'account', 'cardAccount', 'current', 'annual', 'averages', 'investment')
$barclaysId = '11111111-1111-1111-1111-111111111111'
$chaseId = '22222222-2222-2222-2222-222222222222'
$amexId = '33333333-3333-3333-3333-333333333333'
$responses = @{
    'banks'                                       = "[{`"id`":`"$barclaysId`",`"name`":`"Barclays`"},{`"id`":`"$chaseId`",`"name`":`"Chase`"}]"
    'reserve/movements'                           = '[{"bucketName":"Casa","amount":100.00,"date":"2026-08-31T00:00:00"},{"bucketName":"Casa","amount":50.00,"date":"2026-09-01T00:00:00"},{"bucketName":"Viagem","amount":25.50,"date":"2026-09-15T00:00:00"}]'
    'accounts'                                    = "[{`"id`":`"$barclaysId`",`"name`":`"Barclays`",`"templateName`":`"Bank`"},{`"id`":`"$chaseId`",`"name`":`"Chase`",`"templateName`":`"Bank`"},{`"id`":`"$amexId`",`"name`":`"Amex`",`"templateName`":`"CreditCard`",`"isActive`":true},{`"id`":`"44444444-4444-4444-4444-444444444444`",`"name`":`"Reserva`",`"templateName`":`"Pots`"}]"
    'reserve/balances'                            = '[{"bucketName":"Casa","balance":150.00},{"bucketName":"Viagem","balance":25.50}]'
    'controle-mae/entries/totals'                 = '{"totalBrlValue":167.88,"totalGbpValue":7.94}'
    'annual-summary/2026/category-totals'         = '{"categoryTotals":[{"category":"Mercado","annualTotal":1200.00,"average":100.00,"monthlyTotals":[100.00,100.00]}],"incomeSummary":{"salaryAnnualTotal":5000.00,"salaryMonthly":[2500.00,2500.00]},"resultadoAnnualTotal":3800.00,"resultadoMonthly":[1900.00,1900.00]}'
    'annual-summary/2026/historic-summary-averages' = '[{"year":2025,"annualAverages":[{"category":"Mercado","value":95.00}]}]'
    'annual-summary/2026/investment-annual-result' = '{"accounts":[{"account":"ISA","isLiability":false,"monthlyValues":[1000.00,1100.00],"monthlyDiffs":[null,100.00]}],"netPosition":{"monthlyValues":[1000.00,1100.00],"monthlyDiffs":[null,100.00],"averageMonthResult":100.00,"fullYearNetChange":100.00,"sumOfMonthResults":100.00}}'
}
foreach ($month in 8, 9) {
    $route = "2026/$month"
    $responses["banks/month/$route/balances"] = '[{"bank":"Barclays","balance":130.83},{"bank":"Chase","balance":10.00}]'
    $responses["expenses/month/$route"] = "[{`"paymentSourceBankId`":`"$barclaysId`",`"roundUpAmount`":0.17},{`"paymentSourceBankId`":`"$barclaysId`",`"roundUpAmount`":null},{`"paymentSourceBankId`":`"$chaseId`",`"roundUpAmount`":0.50}]"
    $responses["card-statements/$route"] = '[{"creditCardName":"Amex","outstandingTotal":40.10,"accumulatedOutstandingTotal":40.10,"isPaid":false,"warning":null}]'
    $responses["expenses/month/$route/unpaid-card-charges"] = '[{"value":20.05},{"value":20.05}]'
    $responses["expenses/month/$route/category-totals"] = '[{"category":"Mercado","totalValue":0.1},{"category":"Casa","totalValue":0.2}]'
    $responses["incomes/month/$route"] = '[{"incomeSourceName":"Salary","netValue":2000.00,"grossValue":2500.00},{"incomeSourceName":"Salary","netValue":100.00,"grossValue":null},{"incomeSourceName":"Dividends","netValue":12.34,"grossValue":null}]'
    $responses["tithe/month/$route"] = '{"calculatedTithe":211.23,"titheBalance":11.23,"carryForward":{"amount":5.00,"included":true,"fromYear":2026,"fromMonth":7}}'
    $responses["accounts/groups/default/summary/$route"] = "{`"members`":[{`"accountId`":`"$barclaysId`",`"name`":`"Barclays`",`"templateName`":`"Bank`",`"currency`":`"GBP`",`"balance`":130.83,`"roundUpTotal`":0.17},{`"accountId`":`"$chaseId`",`"name`":`"Chase`",`"templateName`":`"Bank`",`"currency`":`"GBP`",`"balance`":10.00,`"roundUpTotal`":0.50},{`"accountId`":`"$amexId`",`"name`":`"Amex`",`"templateName`":`"CreditCard`",`"currency`":`"GBP`",`"balance`":-40.10,`"roundUpTotal`":null,`"statement`":{`"outstandingTotal`":40.10,`"isPaid`":false}}],`"totals`":[{`"currency`":`"GBP`",`"balance`":140.83,`"roundUpTotal`":0.67,`"cardOutstandingTotal`":40.10}],`"categoryTotalsSum`":0.3,`"incomeTotals`":[{`"name`":`"Salary`",`"netValue`":2100.00,`"grossValue`":2500.00},{`"name`":`"Dividends`",`"netValue`":12.34,`"grossValue`":null}],`"totalIncoming`":2112.34}"
    $potsTotal = if ($month -eq 8) { '100.00' } else { '175.50' }
    $responses["accounts/groups/by-template/$route"] = "[{`"templateName`":`"Bank`",`"members`":[{`"name`":`"Barclays`",`"balance`":130.83,`"totalOwed`":null},{`"name`":`"Chase`",`"balance`":10.00,`"totalOwed`":null}],`"totals`":[{`"currency`":`"GBP`",`"total`":140.83},{`"currency`":`"BRL`",`"total`":500.00}]},{`"templateName`":`"CreditCard`",`"members`":[{`"name`":`"Amex`",`"balance`":-40.10,`"totalOwed`":40.10}],`"totals`":[{`"currency`":`"GBP`",`"total`":40.10}]},{`"templateName`":`"Pots`",`"members`":[{`"name`":`"Reserva`",`"balance`":$potsTotal,`"totalOwed`":null}],`"totals`":[{`"currency`":`"GBP`",`"total`":$potsTotal}]}]"
    $responses["accounts/$barclaysId/$route"] = '{"balance":130.83}'
    $responses["accounts/$chaseId/$route"] = '{"balance":10.00}'
    $responses["accounts/$amexId/$route"] = '{"balance":-40.10,"totalOwed":40.10}'
    $responses["investment-snapshots/$route/suggestions"] = '{"suggestions":[{"accountName":"Amex card","suggestedValue":-40.10,"currentValue":0,"sourceDescription":"Invoice"}],"notUpdated":[{"accountName":"Pension","reason":"No source"}]}'
}
$fetch = { param($path) if (-not $responses.ContainsKey($path)) { throw "No fixture for $path" }; ConvertFrom-ParityJson $responses[$path] }

try {
    $report = Get-ParityReport -Fetch $fetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09' -IncludeSummary -IncludeAccounts
    $reportWithoutSummary = Get-ParityReport -Fetch $fetch -First '2026-08' -Last '2026-09'
    $families = @{
        banks      = '{0}/banks/Barclays/balance'
        cards      = '{0}/cards/Amex/outstandingTotal'
        categories = '{0}/categories/Mercado/total'
        incomes    = '{0}/incomes/Salary/net'
        tithe      = '{0}/tithe/titheBalance'
        snapshots  = '{0}/snapshots/Amex card/suggestedValue'
        monthly    = '{0}/monthly/bankTotalsSum'
        pots       = '{0}/pots/total'
        summary    = '{0}/summary/members/Barclays/balance'
        templateGroups = '{0}/templateGroups/Pots/totals/GBP'
        account    = '{0}/account/Chase/balance'
    }
    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($month in '2026-08', '2026-09') {
        foreach ($pattern in $families.Values) { $key = $pattern -f $month; if (-not $report.ContainsKey($key)) { $missing.Add($key) } }
    }
    foreach ($key in 'current/mae/totalBrl', 'current/pots/Casa/balance', '2026/annual/categories/Mercado/annualTotal', '2026/averages/2025/Mercado', '2026/investment/ISA/value/02', '2026/investment/net/fullYearNetChange') {
        if (-not $report.ContainsKey($key)) { $missing.Add($key) }
    }
    foreach ($family in $figureFamilies) {
        if (-not ($report.Keys | Where-Object { $_.Split('/')[1] -eq $family -or $_.Split('/')[0] -eq $family })) { $missing.Add("family $family") }
    }
    Check 'report contains every figure family for every month' ($missing.Count -eq 0) ($missing -join ', ')

    Check 'monthly figures are exact decimal sums' ($report['2026-09/monthly/categoryTotalsSum'] -is [decimal] -and $report['2026-09/monthly/categoryTotalsSum'] -eq [decimal]'0.3') $report['2026-09/monthly/categoryTotalsSum']
    Check 'income gross is null when no income of the source has gross' ($null -eq $report['2026-09/incomes/Dividends/gross'] -and $report['2026-09/incomes/Salary/gross'] -eq [decimal]'2500.00' -and $report['2026-09/incomes/totalIncoming'] -eq [decimal]'2112.34') ''
    Check 'round-up total follows the bank id' ($report['2026-09/banks/Barclays/roundUpTotal'] -eq [decimal]'0.17' -and $report['2026-09/banks/Chase/roundUpTotal'] -eq [decimal]'0.50' -and $report['2026-09/monthly/roundUpTotalsSum'] -eq [decimal]'0.67') ''
    Check 'card figures and unpaid total' ($report['2026-09/cards/unpaidChargesTotal'] -eq [decimal]'40.10' -and $report['2026-09/cards/Amex/isPaid'] -eq $false -and $report['2026-09/monthly/cardOutstandingTotal'] -eq [decimal]'40.10') ''
    Check 'pots balance at month end uses movements up to the last day' ($report['2026-08/pots/Casa/balance'] -eq [decimal]'100.00' -and $report['2026-08/pots/Viagem/balance'] -eq 0 -and $report['2026-09/pots/total'] -eq [decimal]'175.50') ''

    $unfilled = $responses.Clone()
    $unfilled['accounts'] = "[{`"id`":`"$barclaysId`",`"name`":`"Barclays`",`"templateName`":`"Bank`",`"isActive`":true},{`"id`":`"$amexId`",`"name`":`"Amex`",`"templateName`":`"CreditCard`",`"isActive`":true},{`"id`":`"$chaseId`",`"name`":`"Visa`",`"templateName`":`"CreditCard`",`"isActive`":true},{`"id`":`"$chaseId`",`"name`":`"Closed`",`"templateName`":`"CreditCard`",`"isActive`":false}]"
    $unfilled['card-statements/2026/8'] = '[]'
    $unfilled['card-statements/2026/9'] = '[{"creditCardName":"Amex","outstandingTotal":40.10,"accumulatedOutstandingTotal":40.10,"isPaid":false}]'
    $unfilled["accounts/$chaseId/2026/8"] = '{"balance":0,"totalOwed":0}'
    $unfilled["accounts/$chaseId/2026/9"] = '{"balance":0,"totalOwed":0}'
    $unfilled["accounts/$amexId/2026/8"] = '{"balance":0,"totalOwed":40.10}'
    $unfilled["accounts/$amexId/2026/9"] = '{"balance":-40.10,"totalOwed":40.10}'
    $unfilledFetch = { param($path) if (-not $unfilled.ContainsKey($path)) { throw "No fixture for $path" }; ConvertFrom-ParityJson $unfilled[$path] }
    $filled = Get-ParityReport -Fetch $unfilledFetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09' -IncludeAccounts -FillMissingStatements
    $filledBefore = Get-ParityReport -Fetch $unfilledFetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09' -FillMissingStatements
    $notFilled = Get-ParityReport -Fetch $unfilledFetch -First '2026-08' -Last '2026-09'
    Check 'after side fills a missing statement with the card''s accumulated outstanding' ($filled['2026-08/cards/Amex/outstandingTotal'] -eq 0 -and $filled['2026-08/cards/Amex/accumulatedOutstandingTotal'] -eq [decimal]'40.10' -and $filled['2026-08/cards/Amex/isPaid'] -eq $false -and $filled.ContainsKey('2026-08/cards/Amex/warning') -and $null -eq $filled['2026-08/cards/Amex/warning']) ''
    Check 'after side fills a card with no stored statement with zeros' ($filled['2026-08/cards/Visa/accumulatedOutstandingTotal'] -eq 0 -and $filled['2026-09/cards/Visa/outstandingTotal'] -eq 0 -and $filled['2026-09/cards/Visa/isPaid'] -eq $false) ''
    Check 'fill leaves stored statements and inactive cards alone' ($filled['2026-09/cards/Amex/outstandingTotal'] -eq [decimal]'40.10' -and -not $filled.ContainsKey('2026-08/cards/Closed/outstandingTotal')) ''
    Check 'a statement without a warning field reads as a null warning' ($filled.ContainsKey('2026-09/cards/Amex/warning') -and $null -eq $filled['2026-09/cards/Amex/warning']) ''
    Check 'without FillMissingStatements no missing statements are added' (-not $notFilled.ContainsKey('2026-08/cards/Amex/outstandingTotal') -and -not $notFilled.ContainsKey('2026-09/cards/Visa/outstandingTotal')) ''
    Check 'before side fills missing statements without adding account figures' ($filledBefore['2026-08/cards/Amex/accumulatedOutstandingTotal'] -eq [decimal]'40.10' -and $filledBefore['2026-09/cards/Visa/outstandingTotal'] -eq 0 -and -not ($filledBefore.Keys | Where-Object { $_.Split('/')[1] -eq 'account' })) ''

    $duplicate = $responses.Clone()
    $duplicate['banks/month/2026/8/balances'] = '[{"bank":"Barclays","balance":1},{"bank":"Barclays","balance":2}]'
    $duplicateFetch = { param($path) ConvertFrom-ParityJson $duplicate[$path] }
    $message = Get-Error { Get-ParityReport -Fetch $duplicateFetch -First '2026-08' -Last '2026-08' -Side 'after' }
    Check 'duplicate figure key fails' ($message -eq 'Duplicate figure key 2026-08/banks/Barclays/balance on the after side') $message

    $reportPath = Join-Path $work 'roundtrip.json'
    Set-Content -LiteralPath $reportPath -Value (ConvertTo-ParityReportJson $report) -NoNewline
    $roundTrip = Read-ParityReport $reportPath
    Check 'report survives a write and read unchanged' ((Compare-ParityReport $report $roundTrip).Count -eq 0 -and $roundTrip.Count -eq $report.Count) ''

    $beforeFigures = @{ '2026-09/banks/Barclays/balance' = [decimal]'130.83'; '2026-09/cards/Amex/isPaid' = $false; 'current/mae/totalBrl' = [decimal]'167.88' }
    $before = New-Report $beforeFigures
    Check 'compare identical reports passes' ((Compare-ParityReport $before (New-Report $beforeFigures)).Count -eq 0) ''

    $changed = New-Report @{ '2026-09/banks/Barclays/balance' = [decimal]'131.83'; '2026-09/cards/Amex/isPaid' = $false; 'current/mae/totalBrl' = [decimal]'167.88' }
    $differences = Compare-ParityReport $before $changed
    $line = if ($differences.Count -eq 1) { Format-ParityDifference $differences[0] } else { '' }
    Check 'compare reports one changed figure' ($line -eq '2026-09 / banks / Barclays / balance | 130.83 | 131.83') $line

    Check 'compare treats equal decimals of different scale as equal' ((Compare-ParityReport (New-Report @{ a = [decimal]'130.8' }) (New-Report @{ a = [decimal]'130.80' })).Count -eq 0) ''

    $differences = Compare-ParityReport (New-Report @{ a = [decimal]1; onlyBefore = [decimal]2 }) (New-Report @{ a = [decimal]1; onlyAfter = [decimal]3 })
    Check 'compare reports keys missing on either side' ($differences.Count -eq 2 -and ($differences | Where-Object { $_.Key -eq 'onlyBefore' -and $_.After -eq '(missing)' }) -and ($differences | Where-Object { $_.Key -eq 'onlyAfter' -and $_.Before -eq '(missing)' })) ($differences | Out-String)

    Check 'compare reports type mismatch' ((Compare-ParityReport (New-Report @{ a = $false }) (New-Report @{ a = [decimal]0 })).Count -eq 1) ''
    Check 'compare treats null only as equal to null' ((Compare-ParityReport (New-Report @{ a = $null }) (New-Report @{ a = $null })).Count -eq 0 -and (Compare-ParityReport (New-Report @{ a = $null }) (New-Report @{ a = [decimal]0 })).Count -eq 1) ''

    $mapPath = Join-Path $work 'figure-map.json'
    Set-Content -LiteralPath $mapPath -Value '{ "mappings": [ { "after": "{month}/summary/banks/*/balance", "before": "{month}/banks/*/balance", "reason": "test" }, { "after": "{month}/cards/*/balance", "before": "{month}/cards/*/outstandingTotal", "negate": true, "reason": "test" } ] }'
    $mappings = Read-ParityFigureMap $mapPath
    $mappedBefore = New-Report @{ '2026-09/banks/Barclays/balance' = [decimal]'130.83' }
    Check 'mapping renames without allowing a different value' ((Compare-ParityReport $mappedBefore (New-Report @{ '2026-09/summary/banks/Barclays/balance' = [decimal]'130.83' }) $mappings).Count -eq 0 -and (Compare-ParityReport $mappedBefore (New-Report @{ '2026-09/summary/banks/Barclays/balance' = [decimal]'130.84' }) $mappings).Count -eq 1) ''
    Check 'mapping negate compares opposite sign' ((Compare-ParityReport (New-Report @{ '2026-09/cards/Amex/outstandingTotal' = [decimal]'50.00' }) (New-Report @{ '2026-09/cards/Amex/balance' = [decimal]'-50.00' }) $mappings).Count -eq 0) ''
    $templateGroupKeys = @($report.Keys | Where-Object { $_.Split('/')[1] -eq 'templateGroups' } | Sort-Object)
    $expectedTemplateGroupKeys = @(foreach ($month in '2026-08', '2026-09') { "$month/templateGroups/Bank/members/Barclays/balance", "$month/templateGroups/Bank/members/Chase/balance", "$month/templateGroups/Bank/totals/GBP", "$month/templateGroups/CreditCard/members/Amex/totalOwed", "$month/templateGroups/Pots/totals/GBP" }) | Sort-Object
    Check 'template group figures are only those with a baseline equivalent, and only with IncludeSummary' ((Compare-Object $templateGroupKeys $expectedTemplateGroupKeys).Count -eq 0 -and $report['2026-09/templateGroups/Bank/totals/GBP'] -eq [decimal]'140.83' -and $report['2026-09/templateGroups/CreditCard/members/Amex/totalOwed'] -eq [decimal]'40.10' -and $report['2026-09/templateGroups/Pots/totals/GBP'] -eq [decimal]'175.50' -and -not ($reportWithoutSummary.Keys | Where-Object { $_.Split('/')[1] -eq 'templateGroups' })) ($templateGroupKeys -join ', ')
    Check 'summary figures are built only with IncludeSummary' (-not ($reportWithoutSummary.Keys | Where-Object { $_.Split('/')[1] -eq 'summary' }) -and $report['2026-09/summary/totals/GBP/roundUpTotal'] -eq [decimal]'0.67' -and $null -eq $report['2026-09/summary/incomes/Dividends/gross']) ''

    Check 'account figures are built only with IncludeAccounts' (-not ($reportWithoutSummary.Keys | Where-Object { $_.Split('/')[1] -eq 'account' }) -and $report['2026-08/account/Barclays/balance'] -eq [decimal]'130.83' -and $report['2026-09/account/Chase/balance'] -eq [decimal]'10.00') ''

    Check 'card members are built under cardMembers, with their balance only in the current month' ($report['2026-08/summary/cardMembers/Amex/outstandingTotal'] -eq [decimal]'40.10' -and $report['2026-08/summary/cardMembers/Amex/isPaid'] -eq $false -and $report['2026-09/summary/cardMembers/Amex/balance'] -eq [decimal]'-40.10' -and -not $report.ContainsKey('2026-08/summary/cardMembers/Amex/balance') -and -not ($report.Keys | Where-Object { $_ -like '*/summary/members/Amex/*' }) -and $report['2026-09/summary/totals/GBP/cardOutstandingTotal'] -eq [decimal]'40.10') ''

    Check 'account figures skip accounts that are neither banks nor cards' (-not ($report.Keys | Where-Object { $_ -like '*/Reserva/*' })) ''

    Check 'card account balance is built only in the current month' ($report['2026-09/cardAccount/Amex/balance'] -eq [decimal]'-40.10' -and -not $report.ContainsKey('2026-08/cardAccount/Amex/balance') -and -not ($report.Keys | Where-Object { $_ -like '*/account/Amex/*' })) ''

    Check 'card total owed is built in every month for each active card' ($report['2026-08/cardAccount/Amex/totalOwed'] -eq [decimal]'40.10' -and $report['2026-09/cardAccount/Amex/totalOwed'] -eq [decimal]'40.10' -and $filled['2026-08/cardAccount/Visa/totalOwed'] -eq 0 -and -not ($filled.Keys | Where-Object { $_ -like '*/cardAccount/Closed/totalOwed' }) -and $filled.ContainsKey('2026-09/cardAccount/Closed/balance')) ''

    $shippedMappings = Read-ParityFigureMap (Join-Path $here 'figure-map.json')
    $summaryDifferences = Compare-ParityReport $reportWithoutSummary $report $shippedMappings
    Check 'shipped figure map resolves every summary, account and card account key to its before key' ($summaryDifferences.Count -eq 0) (($summaryDifferences | ForEach-Object { Format-ParityDifference $_ }) -join ', ')

    $dataPath = Join-Path $work 'data-cashflow.json'
    Set-Content -LiteralPath $dataPath -Value '{ "Expenses": [ { "Date": "2019-03-04T00:00:00", "ChargeDate": null, "InvoiceDate": "2027-02-01T00:00:00" } ], "Incomes": [ { "Date": "2026-10-14T00:00:00" } ], "InvestmentSnapshots": [ { "Year": 2026, "Month": 12 } ], "CardStatements": [], "Version": 1 }'
    $range = Get-ParityMonthRange $dataPath ([datetime]'2026-10-18')
    $laterNow = Get-ParityMonthRange $dataPath ([datetime]'2027-05-02')
    Check 'month range spans first record to later of now and last record' ($range.First -eq '2019-03' -and $range.Last -eq '2027-02' -and $range.CurrentMonth -eq '2026-10' -and $laterNow.Last -eq '2027-05') "$($range.First)..$($range.Last) / $($laterNow.Last)"

    $repoRoot = Join-Path $work 'repo'
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'data') | Out-Null
    $emptyRoot = Join-Path $work 'empty-repo'
    New-Item -ItemType Directory -Path $emptyRoot | Out-Null
    $missingSourceMessage = Get-Error { Resolve-ParityLiveCopy '' @($emptyRoot) }
    $expectedMissingSource = 'data/data-cashflow.json not found. Download it from Google Drive into data/, or pass -LiveCopy.'
    Set-Content -LiteralPath (Join-Path $repoRoot 'data/data-cashflow.json') -Value '{ "Version": 3 }'
    Set-Content -LiteralPath (Join-Path $repoRoot 'data/copy.json') -Value '{}'
    $liveFile = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'data/data-cashflow.json'))
    Push-Location $repoRoot
    try {
        $defaultSource = Resolve-ParityLiveCopy '' @($repoRoot)
        $worktreeSource = Resolve-ParityLiveCopy '' @($emptyRoot, $repoRoot)
        $explicitSource = Resolve-ParityLiveCopy 'data/copy.json' @($repoRoot)
        $missingCopyMessage = Get-Error { Resolve-ParityLiveCopy 'data/none.json' @($repoRoot) }
    }
    finally { Pop-Location }
    Check 'source defaults to data/data-cashflow.json, also from a worktree, and a missing source is named' ($defaultSource -eq $liveFile -and $worktreeSource -eq $liveFile -and $explicitSource -like '*copy.json' -and $missingSourceMessage -eq $expectedMissingSource -and $missingCopyMessage -match '^Live copy not found') "$defaultSource / $worktreeSource / $explicitSource / $missingSourceMessage / $missingCopyMessage"

    $sourceHash = Get-ParitySha256 $liveFile
    $sourceCopy = Copy-ParitySource $liveFile $work
    Add-Content -LiteralPath $sourceCopy -Value ' '
    $sourceLine = Format-ParitySource $liveFile
    Check 'the run works on a copy and leaves the source unchanged' ((Get-ParitySha256 $liveFile) -eq $sourceHash -and $sourceCopy -eq (Join-Path $work 'source.json') -and $sourceLine -match "version 3, SHA-256 $($sourceHash.Substring(0, 8))") $sourceLine

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try {
        $busyPort = $listener.LocalEndpoint.Port
        $portMessage = 'is in use (or is the live port 8080)'
        $liveMessage = Get-Error { Assert-ParityPorts @(8080, 5292) }
        $busyMessage = Get-Error { Assert-ParityPorts @($busyPort, 5292) }
        $equalMessage = Get-Error { Assert-ParityPorts @(5293, 5293) }
    }
    finally { $listener.Stop() }
    $freeListener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $freeListener.Start(); $freePort = $freeListener.LocalEndpoint.Port; $freeListener.Stop()
    $freeMessage = Get-Error { Assert-ParityPorts @($freePort) }
    Check 'port guard refuses 8080 busy and equal ports' ($liveMessage -match [regex]::Escape($portMessage) -and $busyMessage -match "Port $busyPort " -and $equalMessage -match 'Port 5293 ' -and $freeMessage -eq '') "$liveMessage / $busyMessage / $equalMessage / $freeMessage"

    $hash = Get-ParitySha256 $dataPath
    $unchangedMessage = Get-Error { Assert-ParityHashUnchanged $dataPath $hash }
    Add-Content -LiteralPath $dataPath -Value ' '
    $changedMessage = Get-Error { Assert-ParityHashUnchanged $dataPath $hash }
    Check 'hash change is detected' ($unchangedMessage -eq '' -and $changedMessage -eq 'The input copy changed during the run; results discarded.') $changedMessage

    $runFolder = Join-Path $work 'run'
    New-Item -ItemType Directory -Path $runFolder | Out-Null
    Set-Content -LiteralPath (Join-Path $runFolder 'before-report.json') -Value (ConvertTo-ParityReportJson $before) -NoNewline
    Set-Content -LiteralPath (Join-Path $runFolder 'after-report.json') -Value (ConvertTo-ParityReportJson $before) -NoNewline
    $output = & pwsh -NoProfile -File $entry -Step Compare -WorkFolder $runFolder 2>&1 | Out-String
    Check 'compare step passes on identical reports' ($LASTEXITCODE -eq 0 -and $output -match 'Parity OK: 0 differences in 3 figures across 1 months') $output

    Set-Content -LiteralPath (Join-Path $runFolder 'after-report.json') -Value (ConvertTo-ParityReportJson $changed) -NoNewline
    $output = & pwsh -NoProfile -File $entry -Step Compare -WorkFolder $runFolder 2>&1 | Out-String
    Check 'compare step fails and lists the changed figure' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape('2026-09 / banks / Barclays / balance | 130.83 | 131.83') -and $output -match 'Parity FAILED: 1 difference\b' -and (Test-Path (Join-Path $runFolder 'differences.txt'))) $output

    $output = & pwsh -NoProfile -File $entry -Step All -RepoRoot $emptyRoot 2>&1 | Out-String
    Check 'entry script stops before anything runs when there is no source' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape($expectedMissingSource) -and $output -notmatch 'Work folder') $output

    git init --quiet $repoRoot
    $copyPath = Join-Path $repoRoot 'data/copy.json'
    $output = & pwsh -NoProfile -File $entry -Step Baseline -LiveCopy $copyPath -RepoRoot $repoRoot -BeforePort $freePort 2>&1 | Out-String
    Check 'entry script refuses a missing baseline tag before anything runs' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape('Tag p60-deployed-f14 not found. Create it on main before the first run.') -and $output -notmatch 'Work folder') $output

    $output = & pwsh -NoProfile -File $entry -Step All -LiveCopy $copyPath -RepoRoot $repoRoot -BeforePort 8080 2>&1 | Out-String
    Check 'entry script refuses port 8080 before anything runs' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape('Port 8080 is in use (or is the live port 8080)') -and $output -notmatch 'Work folder') $output

    $badMapPath = Join-Path $work 'bad-map.json'
    Set-Content -LiteralPath $badMapPath -Value '{ "mappings": [ { "after": "{month}/x/*", "befor": "{month}/y/*" }, { "after": "{month}/x/*", "before": "{month}/y" } ] }'
    $badMappings = Read-ParityFigureMap $badMapPath
    $missingBefore = Get-Error { Compare-ParityReport (New-Report @{}) (New-Report @{}) @($badMappings[0]) }
    $placeholderMismatch = Get-Error { Compare-ParityReport (New-Report @{}) (New-Report @{}) @($badMappings[1]) }
    Check 'malformed figure-map entries are named' ($missingBefore -match '^Invalid figure-map entry' -and $placeholderMismatch -match '^Invalid figure-map entry') "$missingBefore / $placeholderMismatch"

    $readme = Get-Content -LiteralPath (Join-Path $here 'README.md') -Raw
    $undocumented = $figureFamilies + @('Baseline', 'Migrate', 'After', 'Compare', 'All', 'KeepRunning') | Where-Object { $readme -notmatch "\b$_\b" }
    Check 'readme documents every catalogue family and step' (@($undocumented).Count -eq 0 -and (Test-Path (Join-Path $here 'screen-checklist.md'))) ($undocumented -join ', ')
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "$($cases - $failures)/$cases parity toolkit cases passed"
if ($failures -gt 0) { exit 1 }
