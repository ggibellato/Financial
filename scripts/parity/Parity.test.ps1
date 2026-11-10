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

$figureFamilies = @('categories', 'incomes', 'tithe', 'snapshots', 'monthly', 'potsTab', 'summary', 'templateGroups', 'account', 'cardAccount', 'cardStatement', 'monthTransactions', 'settlement', 'current', 'annual', 'averages', 'investment')
$mae = "M$([char]0xE3)e"
$barclaysId = '11111111-1111-1111-1111-111111111111'
$chaseId = '22222222-2222-2222-2222-222222222222'
$amexId = '33333333-3333-3333-3333-333333333333'
$reservaId = '44444444-4444-4444-4444-444444444444'
$maeId = '88888888-8888-8888-8888-888888888888'
$otherPotsId = '99999999-9999-9999-9999-999999999999'
$closedId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
$amexStatementId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
$responses = @{
    'accounts'                                    = "[{`"id`":`"$barclaysId`",`"name`":`"Barclays`",`"templateName`":`"Bank`"},{`"id`":`"$chaseId`",`"name`":`"Chase`",`"templateName`":`"Bank`"},{`"id`":`"$amexId`",`"name`":`"Amex`",`"templateName`":`"CreditCard`",`"isActive`":true},{`"id`":`"$otherPotsId`",`"name`":`"Ferias`",`"templateName`":`"Pots`"},{`"id`":`"$reservaId`",`"name`":`"Reserva`",`"templateName`":`"Pots`",`"replacesPage`":`"reserva`"},{`"id`":`"$maeId`",`"name`":`"$mae`",`"templateName`":`"Settlement`",`"replacesPage`":`"controle-mae`"}]"
    'annual-summary/2026/category-totals'         = '{"categoryTotals":[{"category":"Mercado","annualTotal":1200.00,"average":100.00,"monthlyTotals":[100.00,100.00]}],"incomeSummary":{"salaryAnnualTotal":5000.00,"salaryMonthly":[2500.00,2500.00]},"resultadoAnnualTotal":3800.00,"resultadoMonthly":[1900.00,1900.00]}'
    'annual-summary/2026/historic-summary-averages' = '[{"year":2025,"annualAverages":[{"category":"Mercado","value":95.00}]}]'
    'annual-summary/2026/investment-annual-result' = '{"accounts":[{"account":"ISA","isLiability":false,"monthlyValues":[1000.00,1100.00],"monthlyDiffs":[null,100.00]}],"netPosition":{"monthlyValues":[1000.00,1100.00],"monthlyDiffs":[null,100.00],"averageMonthResult":100.00,"fullYearNetChange":100.00,"sumOfMonthResults":100.00}}'
}
$roundUpRows = "{`"accountId`":`"$barclaysId`",`"accountName`":`"Barclays`",`"transaction`":{`"kind`":`"Expense`",`"categoryName`":null,`"amount`":-9.83,`"roundUp`":-0.17}},{`"accountId`":`"$barclaysId`",`"accountName`":`"Barclays`",`"transaction`":{`"kind`":`"Income`",`"categoryName`":null,`"amount`":2000.00,`"roundUp`":null}},{`"accountId`":`"$chaseId`",`"accountName`":`"Chase`",`"transaction`":{`"kind`":`"Expense`",`"categoryName`":null,`"amount`":-4.50,`"roundUp`":-0.50}}"
$cardChargeRows = "{`"accountId`":`"$amexId`",`"accountName`":`"Amex`",`"transaction`":{`"kind`":`"CardCharge`",`"categoryName`":null,`"amount`":-30.00,`"expense`":{`"creditCardId`":`"$amexId`",`"paymentStatus`":`"CreditCardCharge`",`"value`":30.00}}},{`"accountId`":`"$amexId`",`"accountName`":`"Amex`",`"transaction`":{`"kind`":`"CardRefund`",`"categoryName`":null,`"amount`":5.00,`"expense`":{`"creditCardId`":`"$amexId`",`"paymentStatus`":`"CreditCardCharge`",`"value`":-5.00}}},{`"accountId`":`"$amexId`",`"accountName`":`"Amex`",`"transaction`":{`"kind`":`"CardCharge`",`"categoryName`":null,`"amount`":-7.00,`"expense`":{`"creditCardId`":`"$amexId`",`"paymentStatus`":`"CreditCardStatementPaid`",`"value`":7.00}}},{`"accountId`":`"$amexId`",`"accountName`":`"Amex`",`"transaction`":{`"kind`":`"CardCharge`",`"categoryName`":null,`"amount`":-3.00,`"expense`":{`"creditCardId`":`"$amexId`",`"paymentStatus`":`"CreditCardSettled`",`"value`":3.00}}},{`"accountId`":`"$chaseId`",`"accountName`":`"Chase`",`"transaction`":{`"kind`":`"Expense`",`"categoryName`":null,`"amount`":-2.00,`"expense`":{`"creditCardId`":null,`"paymentStatus`":`"CreditCardCharge`",`"value`":2.00}}}"
foreach ($month in 8, 9) {
    $route = "2026/$month"
    $responses["expenses/month/$route/category-totals"] = '[{"category":"Mercado","totalValue":0.1},{"category":"Casa","totalValue":0.2}]'
    $responses["incomes/month/$route"] = '[{"incomeSourceName":"Salary","netValue":2000.00,"grossValue":2500.00},{"incomeSourceName":"Salary","netValue":100.00,"grossValue":null},{"incomeSourceName":"Dividends","netValue":12.34,"grossValue":null}]'
    $responses["tithe/month/$route"] = '{"calculatedTithe":211.23,"titheBalance":11.23,"carryForward":{"amount":5.00,"included":true,"fromYear":2026,"fromMonth":7}}'
    $responses["accounts/groups/default/summary/$route"] = "{`"members`":[{`"accountId`":`"$barclaysId`",`"name`":`"Barclays`",`"templateName`":`"Bank`",`"currency`":`"GBP`",`"balance`":130.83,`"roundUpTotal`":0.17},{`"accountId`":`"$chaseId`",`"name`":`"Chase`",`"templateName`":`"Bank`",`"currency`":`"GBP`",`"balance`":10.00,`"roundUpTotal`":0.50},{`"accountId`":`"$amexId`",`"name`":`"Amex`",`"templateName`":`"CreditCard`",`"currency`":`"GBP`",`"balance`":-40.10,`"roundUpTotal`":null,`"statement`":{`"outstandingTotal`":40.10,`"isPaid`":false}}],`"totals`":[{`"currency`":`"GBP`",`"balance`":140.83,`"roundUpTotal`":0.67,`"cardOutstandingTotal`":40.10}],`"categoryTotalsSum`":0.3,`"incomeTotals`":[{`"name`":`"Salary`",`"netValue`":2100.00,`"grossValue`":2500.00},{`"name`":`"Dividends`",`"netValue`":12.34,`"grossValue`":null}],`"totalIncoming`":2112.34}"
    $potsTotal = if ($month -eq 8) { '100.00' } else { '175.50' }
    $maeBrl = if ($month -eq 8) { '70.50' } else { '137.88' }
    $maeGbp = if ($month -eq 8) { '15.25' } else { '7.94' }
    $settlementGroup = ",{`"templateName`":`"Settlement`",`"members`":[{`"name`":`"$mae`",`"balance`":$maeBrl,`"totalOwed`":null,`"currencyBalances`":[{`"currency`":`"BRL`",`"balance`":$maeBrl},{`"currency`":`"GBP`",`"balance`":$maeGbp}]}],`"totals`":[]}"
    $responses["accounts/groups/by-template/$route"] = "[{`"templateName`":`"Bank`",`"members`":[{`"name`":`"Barclays`",`"balance`":130.83,`"totalOwed`":null},{`"name`":`"Chase`",`"balance`":10.00,`"totalOwed`":null}],`"totals`":[{`"currency`":`"GBP`",`"total`":140.83},{`"currency`":`"BRL`",`"total`":500.00}]},{`"templateName`":`"CreditCard`",`"members`":[{`"name`":`"Amex`",`"balance`":-40.10,`"totalOwed`":40.10}],`"totals`":[{`"currency`":`"GBP`",`"total`":40.10}]},{`"templateName`":`"Pots`",`"members`":[{`"name`":`"Reserva`",`"balance`":$potsTotal,`"totalOwed`":null}],`"totals`":[{`"currency`":`"GBP`",`"total`":$potsTotal}]}$settlementGroup]"
    $casaBalance = if ($month -eq 8) { '100.00' } else { '150.00' }
    $viagemBalance = if ($month -eq 8) { '0' } else { '25.50' }
    $responses["accounts/$reservaId/pots/$route"] = "{`"potsAccountId`":`"$reservaId`",`"currency`":`"GBP`",`"total`":$potsTotal,`"pots`":[{`"id`":`"55555555-5555-5555-5555-555555555555`",`"name`":`"Casa`",`"splitPercentage`":60,`"isActive`":true,`"balance`":$casaBalance},{`"id`":`"66666666-6666-6666-6666-666666666666`",`"name`":`"Viagem`",`"splitPercentage`":40,`"isActive`":true,`"balance`":$viagemBalance},{`"id`":`"77777777-7777-7777-7777-777777777777`",`"name`":`"Novo`",`"splitPercentage`":0,`"isActive`":false,`"balance`":0}]}"
    $responses["accounts/$barclaysId/$route"] = '{"balance":130.83}'
    $responses["accounts/$chaseId/$route"] = '{"balance":10.00}'
    $responses["accounts/$amexId/$route"] = "{`"balance`":-40.10,`"totalOwed`":40.10,`"viewedStatement`":{`"id`":`"$amexStatementId`",`"outstandingTotal`":40.10,`"isPaid`":false}}"
    $responses["accounts/transactions/$route"] = "[{`"accountName`":`"Barclays`",`"transaction`":{`"kind`":`"Expense`",`"categoryName`":`"Mercado`",`"amount`":-0.1}},{`"accountName`":`"Amex`",`"transaction`":{`"kind`":`"CardCharge`",`"categoryName`":`"Casa`",`"amount`":-0.25}},{`"accountName`":`"Amex`",`"transaction`":{`"kind`":`"CardRefund`",`"categoryName`":`"Casa`",`"amount`":0.05}},{`"accountName`":`"Barclays`",`"transaction`":{`"kind`":`"Income`",`"categoryName`":null,`"amount`":2000.00}},{`"accountName`":`"Chase`",`"transaction`":{`"kind`":`"TransferIn`",`"amount`":40.00}},$roundUpRows,$cardChargeRows]"
    $responses["accounts/$maeId/$route"] = '{"currentBalances":[{"currency":"BRL","balance":167.88},{"currency":"GBP","balance":7.94}]}'
    $responses["investment-snapshots/$route/suggestions"] = '{"suggestions":[{"accountName":"Amex card","suggestedValue":-40.10,"currentValue":0,"sourceDescription":"Invoice"}],"notUpdated":[{"accountName":"Pension","reason":"No source"}]}'
}
$fetch = { param($path) if (-not $responses.ContainsKey($path)) { throw "No fixture for $path" }; ConvertFrom-ParityJson $responses[$path] }

try {
    $report = Get-ParityReport -Fetch $fetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09'
    $families = @{
        cardStatement = '{0}/cardStatement/Amex/outstandingTotal'
        categories = '{0}/categories/Mercado/total'
        incomes    = '{0}/incomes/Salary/net'
        tithe      = '{0}/tithe/titheBalance'
        snapshots  = '{0}/snapshots/Amex card/suggestedValue'
        monthly    = '{0}/monthly/categoryTotalsSum'
        potsTab    = '{0}/potsTab/total'
        summary    = '{0}/summary/members/Barclays/balance'
        templateGroups = '{0}/templateGroups/Pots/totals/GBP'
        account    = '{0}/account/Chase/balance'
        monthTransactions = '{0}/monthTransactions/categories/Casa/spend'
        settlement = "{0}/settlement/$mae/GBP"
    }
    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($month in '2026-08', '2026-09') {
        foreach ($pattern in $families.Values) { $key = $pattern -f $month; if (-not $report.ContainsKey($key)) { $missing.Add($key) } }
    }
    foreach ($key in 'current/maeAccount/totalBrl', 'current/potsTab/Casa/balance', '2026/annual/categories/Mercado/annualTotal', '2026/averages/2025/Mercado', '2026/investment/ISA/value/02', '2026/investment/net/fullYearNetChange') {
        if (-not $report.ContainsKey($key)) { $missing.Add($key) }
    }
    foreach ($family in $figureFamilies) {
        if (-not ($report.Keys | Where-Object { $_.Split('/')[1] -eq $family -or $_.Split('/')[0] -eq $family })) { $missing.Add("family $family") }
    }
    Check 'the report contains every figure family for every month' ($missing.Count -eq 0) ($missing -join ', ')

    Check 'monthly figures are exact decimal sums' ($report['2026-09/monthly/categoryTotalsSum'] -is [decimal] -and $report['2026-09/monthly/categoryTotalsSum'] -eq [decimal]'0.3') $report['2026-09/monthly/categoryTotalsSum']
    Check 'income gross is null when no income of the source has gross' ($null -eq $report['2026-09/incomes/Dividends/gross'] -and $report['2026-09/incomes/Salary/gross'] -eq [decimal]'2500.00' -and $report['2026-09/incomes/totalIncoming'] -eq [decimal]'2112.34') ''
    Check 'no figure of a retired family is built' (-not ($report.Keys | Where-Object { $_.Split('/')[1] -in 'banks', 'cards', 'pots', 'mae' -or $_ -like '*/monthly/bankTotalsSum' -or $_ -like '*/monthly/roundUpTotalsSum' -or $_ -like '*/monthly/cardOutstandingTotal' })) ''
    Check 'a bank account''s round-up total is minus the sum of its row round-ups' ($report['2026-08/account/Barclays/roundUpTotal'] -eq [decimal]'0.17' -and $report['2026-09/account/Chase/roundUpTotal'] -eq [decimal]'0.50' -and $report['2026-09/account/Chase/roundUpTotal'] -is [decimal] -and -not ($report.Keys | Where-Object { $_ -like '*/account/Amex/roundUpTotal' })) ''
    Check 'a card''s statement figures come from its account detail' ($report['2026-08/cardStatement/Amex/outstandingTotal'] -eq [decimal]'40.10' -and $report['2026-09/cardStatement/Amex/accumulatedOutstandingTotal'] -eq [decimal]'40.10' -and $report['2026-09/cardStatement/Amex/isPaid'] -eq $false) ''
    Check 'no card warning figure is built' (-not ($report.Keys | Where-Object { $_ -like '*/warning' })) ''

    $unfilled = $responses.Clone()
    $unfilled['accounts'] = "[{`"id`":`"$barclaysId`",`"name`":`"Barclays`",`"templateName`":`"Bank`",`"isActive`":true},{`"id`":`"$amexId`",`"name`":`"Amex`",`"templateName`":`"CreditCard`",`"isActive`":true},{`"id`":`"$chaseId`",`"name`":`"Visa`",`"templateName`":`"CreditCard`",`"isActive`":true},{`"id`":`"$closedId`",`"name`":`"Closed`",`"templateName`":`"CreditCard`",`"isActive`":false}]"
    $unfilled["accounts/$chaseId/2026/8"] = '{"balance":0,"totalOwed":0}'
    $unfilled["accounts/$chaseId/2026/9"] = '{"balance":0,"totalOwed":0}'
    $unfilled["accounts/$amexId/2026/8"] = '{"balance":0,"totalOwed":40.10}'
    $unfilled["accounts/$amexId/2026/9"] = $responses["accounts/$amexId/2026/9"]
    $unfilled["accounts/$closedId/2026/8"] = "{`"balance`":0,`"totalOwed`":0,`"viewedStatement`":{`"id`":`"$closedId`",`"outstandingTotal`":12.50,`"isPaid`":true}}"
    $unfilled["accounts/$closedId/2026/9"] = '{"balance":0,"totalOwed":0,"viewedStatement":{"id":null,"outstandingTotal":0,"isPaid":false}}'
    $unfilledFetch = { param($path) if (-not $unfilled.ContainsKey($path)) { throw "No fixture for $path" }; ConvertFrom-ParityJson $unfilled[$path] }
    $filled = Get-ParityReport -Fetch $unfilledFetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09'
    Check 'a missing statement is filled under cardStatement with the card''s accumulated outstanding' ($filled['2026-08/cardStatement/Amex/outstandingTotal'] -eq 0 -and $filled['2026-08/cardStatement/Amex/accumulatedOutstandingTotal'] -eq [decimal]'40.10' -and $filled['2026-08/cardStatement/Amex/isPaid'] -eq $false) ''
    Check 'a card with no stored statement is filled with zeros' ($filled['2026-08/cardStatement/Visa/accumulatedOutstandingTotal'] -eq 0 -and $filled['2026-09/cardStatement/Visa/outstandingTotal'] -eq 0 -and $filled['2026-09/cardStatement/Visa/isPaid'] -eq $false) ''
    Check 'fill leaves stored statements and inactive cards alone' ($filled['2026-09/cardStatement/Amex/outstandingTotal'] -eq [decimal]'40.10' -and -not $filled.ContainsKey('2026-09/cardStatement/Closed/outstandingTotal')) ''
    Check 'an inactive card''s stored statement is read in any month' ($filled['2026-08/cardStatement/Closed/outstandingTotal'] -eq [decimal]'12.50' -and $filled['2026-08/cardStatement/Closed/isPaid'] -eq $true -and $filled['2026-08/cardStatement/Closed/accumulatedOutstandingTotal'] -eq 0) ''

    $duplicate = $responses.Clone()
    $duplicate['expenses/month/2026/8/category-totals'] = '[{"category":"Mercado","totalValue":1},{"category":"Mercado","totalValue":2}]'
    $duplicateFetch = { param($path) ConvertFrom-ParityJson $duplicate[$path] }
    $message = Get-Error { Get-ParityReport -Fetch $duplicateFetch -First '2026-08' -Last '2026-08' -CurrentMonth '2026-08' -Side 'after' }
    Check 'duplicate figure key fails' ($message -eq 'Duplicate figure key 2026-08/categories/Mercado/total on the after side') $message

    $reportPath = Join-Path $work 'roundtrip.json'
    Set-Content -LiteralPath $reportPath -Value (ConvertTo-ParityReportJson $report) -NoNewline
    $roundTrip = Read-ParityReport $reportPath
    Check 'report survives a write and read unchanged' ((Compare-ParityReport $report $roundTrip).Count -eq 0 -and $roundTrip.Count -eq $report.Count) ''

    $beforeFigures = @{ '2026-09/account/Barclays/balance' = [decimal]'130.83'; '2026-09/cardStatement/Amex/isPaid' = $false; 'current/maeAccount/totalBrl' = [decimal]'167.88' }
    $before = New-Report $beforeFigures
    Check 'compare identical reports passes' ((Compare-ParityReport $before (New-Report $beforeFigures)).Count -eq 0) ''

    $changed = New-Report @{ '2026-09/account/Barclays/balance' = [decimal]'131.83'; '2026-09/cardStatement/Amex/isPaid' = $false; 'current/maeAccount/totalBrl' = [decimal]'167.88' }
    $differences = Compare-ParityReport $before $changed
    $line = if ($differences.Count -eq 1) { Format-ParityDifference $differences[0] } else { '' }
    Check 'compare reports one changed figure' ($line -eq '2026-09 / account / Barclays / balance | 130.83 | 131.83') $line

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

    $shippedMappings = Read-ParityFigureMap (Join-Path $here 'figure-map.json')
    Check 'the shipped figure map has no mappings, so each figure is compared with the same key' ($shippedMappings.Count -eq 0) ($shippedMappings | ConvertTo-Json -Compress)
    $uncompared = foreach ($key in $report.Keys) {
        $value = $report[$key]
        $changedValue = if ($null -eq $value) { [decimal]1 } elseif ($value -is [bool]) { -not $value } elseif ($value -is [decimal]) { $value + [decimal]'0.01' } else { "$value!" }
        $changedReport = New-Report $report
        $changedReport[$key] = $changedValue
        foreach ($pair in @(@($report, $changedReport), @($changedReport, $report))) {
            $found = @(Compare-ParityReport $pair[0] $pair[1] $shippedMappings)
            if ($found.Count -ne 1 -or $found[0].Key -ne $key) { $key }
        }
    }
    Check 'a changed figure on either side is reported under its own key, for every figure' (@($uncompared).Count -eq 0 -and $report.Count -gt 100) (@($uncompared) -join ', ')
    Check 'identical reports compare clean through the shipped figure map' ((Compare-ParityReport $report (New-Report $report) $shippedMappings).Count -eq 0) ''

    $templateGroupKeys = @($report.Keys | Where-Object { $_.Split('/')[1] -eq 'templateGroups' } | Sort-Object)
    $expectedTemplateGroupKeys = @(foreach ($month in '2026-08', '2026-09') { "$month/templateGroups/Bank/members/Barclays/balance", "$month/templateGroups/Bank/members/Chase/balance", "$month/templateGroups/Bank/totals/GBP", "$month/templateGroups/CreditCard/members/Amex/totalOwed", "$month/templateGroups/Pots/totals/GBP" }) | Sort-Object
    Check 'template group figures are the bank members and totals, card total owed and Pots total' ((Compare-Object $templateGroupKeys $expectedTemplateGroupKeys).Count -eq 0 -and $report['2026-09/templateGroups/Bank/totals/GBP'] -eq [decimal]'140.83' -and $report['2026-09/templateGroups/CreditCard/members/Amex/totalOwed'] -eq [decimal]'40.10' -and $report['2026-09/templateGroups/Pots/totals/GBP'] -eq [decimal]'175.50') ($templateGroupKeys -join ', ')
    Check 'summary figures come from the default group summary' ($report['2026-09/summary/totals/GBP/roundUpTotal'] -eq [decimal]'0.67' -and $null -eq $report['2026-09/summary/incomes/Dividends/gross']) ''

    Check 'account figures come from each bank account''s detail' ($report['2026-08/account/Barclays/balance'] -eq [decimal]'130.83' -and $report['2026-09/account/Chase/balance'] -eq [decimal]'10.00') ''

    Check 'card members are built under cardMembers, with their balance only in the current month' ($report['2026-08/summary/cardMembers/Amex/outstandingTotal'] -eq [decimal]'40.10' -and $report['2026-08/summary/cardMembers/Amex/isPaid'] -eq $false -and $report['2026-09/summary/cardMembers/Amex/balance'] -eq [decimal]'-40.10' -and -not $report.ContainsKey('2026-08/summary/cardMembers/Amex/balance') -and -not ($report.Keys | Where-Object { $_ -like '*/summary/members/Amex/*' }) -and $report['2026-09/summary/totals/GBP/cardOutstandingTotal'] -eq [decimal]'40.10') ''

    Check 'pots tab figures come from the account that replaces the Reserva page, non-zero pots only' ($report['2026-08/potsTab/Casa/balance'] -eq [decimal]'100.00' -and -not $report.ContainsKey('2026-08/potsTab/Viagem/balance') -and $report['2026-08/potsTab/total'] -eq [decimal]'100.00' -and $report['2026-09/potsTab/Viagem/balance'] -eq [decimal]'25.50' -and $report['2026-09/potsTab/total'] -eq [decimal]'175.50') ''

    Check 'no pots tab figures without a Pots account' (-not ($filled.Keys | Where-Object { $_.Split('/')[1] -eq 'potsTab' })) ''

    $unusedPot = $responses.Clone()
    $unusedPot["accounts/$reservaId/pots/2026/9"] = $responses["accounts/$reservaId/pots/2026/9"].Replace('"name":"Novo","splitPercentage":0,"isActive":false,"balance":0', '"name":"Novo","splitPercentage":0,"isActive":false,"balance":5')
    $unusedPotFetch = { param($path) ConvertFrom-ParityJson $unusedPot[$path] }
    $unusedPotReport = Get-ParityReport -Fetch $unusedPotFetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09'
    $unusedPotDifferences = Compare-ParityReport $report $unusedPotReport $shippedMappings
    Check 'a pot at zero is skipped and reported, also in the current balances, when it has a balance' (-not ($report.Keys | Where-Object { $_ -like '????-??/potsTab/Novo/*' }) -and $unusedPotDifferences.Count -eq 2 -and $unusedPotDifferences[0].Key -eq '2026-09/potsTab/Novo/balance' -and $unusedPotDifferences[1].Key -eq 'current/potsTab/Novo/balance' -and $unusedPotDifferences[1].Before -eq '0') (($unusedPotDifferences | ForEach-Object { Format-ParityDifference $_ }) -join ', ')

    Check 'every Reserva pot''s current balance is read from the Pots tab at the run''s last month' ($report['current/potsTab/Casa/balance'] -eq [decimal]'150.00' -and $report['current/potsTab/Viagem/balance'] -eq [decimal]'25.50' -and $report['current/potsTab/Novo/balance'] -eq 0) ''

    Check 'account figures skip accounts that are neither banks nor cards' (-not ($report.Keys | Where-Object { $_ -like '*/Reserva/*' })) ''

    Check 'card account balance is built only in the current month' ($report['2026-09/cardAccount/Amex/balance'] -eq [decimal]'-40.10' -and -not $report.ContainsKey('2026-08/cardAccount/Amex/balance') -and -not ($report.Keys | Where-Object { $_ -like '*/account/Amex/*' })) ''

    Check 'card total owed is built in every month for each active card' ($report['2026-08/cardAccount/Amex/totalOwed'] -eq [decimal]'40.10' -and $report['2026-09/cardAccount/Amex/totalOwed'] -eq [decimal]'40.10' -and $filled['2026-08/cardAccount/Visa/totalOwed'] -eq 0 -and -not ($filled.Keys | Where-Object { $_ -like '*/cardAccount/Closed/totalOwed' }) -and $filled.ContainsKey('2026-09/cardAccount/Closed/balance')) ''

    $monthTransactionKeys = @($report.Keys | Where-Object { $_ -like '2026-09/monthTransactions/categories/*' })
    Check 'month transactions spend per category is minus the sum of its rows and ignores rows without a category' ($monthTransactionKeys.Count -eq 2 -and $report['2026-09/monthTransactions/categories/Mercado/spend'] -eq [decimal]'0.1' -and $report['2026-09/monthTransactions/categories/Casa/spend'] -eq [decimal]'0.2' -and $report['2026-09/monthTransactions/categories/Casa/spend'] -is [decimal]) ($monthTransactionKeys -join ', ')
    Check 'the unpaid card charges total sums the card rows still charged, refunds included' ($report['2026-08/monthTransactions/unpaidCardChargesTotal'] -eq [decimal]'25.00' -and $report['2026-09/monthTransactions/unpaidCardChargesTotal'] -is [decimal]) ''
    $noRows = $responses.Clone()
    $noRows['accounts/transactions/2026/9'] = '[]'
    $noRowsReport = Get-ParityReport -Fetch { param($path) ConvertFrom-ParityJson $noRows[$path] } -First '2026-09' -Last '2026-09' -CurrentMonth '2026-09'
    Check 'a month without card charges has an unpaid card charges total of 0' ($noRowsReport['2026-09/monthTransactions/unpaidCardChargesTotal'] -eq 0) ''

    $compareGridVariant = { param($rows)
        $variant = $responses.Clone()
        $variant['accounts/transactions/2026/9'] = $rows.TrimEnd(']') + ",$roundUpRows,$cardChargeRows]"
        $variantFetch = { param($path) ConvertFrom-ParityJson $variant[$path] }.GetNewClosure()
        $variantReport = Get-ParityReport -Fetch $variantFetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09'
        , @(Compare-ParityReport $report $variantReport $shippedMappings)
    }
    $lackingCasa = & $compareGridVariant '[{"transaction":{"categoryName":"Mercado","amount":-0.1}}]'
    Check 'a category Category totals list that the grid lacks is emitted as 0, so a dropped category is a difference' ($lackingCasa.Count -eq 1 -and $lackingCasa[0].Key -eq '2026-09/monthTransactions/categories/Casa/spend' -and $lackingCasa[0].Before -eq '0.20' -and $lackingCasa[0].After -eq '0') (($lackingCasa | ForEach-Object { Format-ParityDifference $_ }) -join ', ')
    $gridOnly = & $compareGridVariant '[{"transaction":{"categoryName":"Mercado","amount":-0.1}},{"transaction":{"categoryName":"Casa","amount":-0.2}},{"transaction":{"categoryName":"Lazer","amount":-9}}]'
    Check 'a category only one side''s grid has shows as missing on the other side' ($gridOnly.Count -eq 1 -and $gridOnly[0].Key -eq '2026-09/monthTransactions/categories/Lazer/spend' -and $gridOnly[0].Before -eq '(missing)') (($gridOnly | ForEach-Object { Format-ParityDifference $_ }) -join ', ')

    $settlementKeys = { param($r) @($r.Keys | Where-Object { $_.Split('/')[1] -eq 'settlement' } | Sort-Object) }
    $expectedSettlementKeys = @(foreach ($month in '2026-08', '2026-09') { "$month/settlement/$mae/BRL", "$month/settlement/$mae/GBP" }) | Sort-Object
    Check 'settlement figures are built for every month' ((Compare-Object (& $settlementKeys $report) $expectedSettlementKeys).Count -eq 0) ((& $settlementKeys $report) -join ', ')
    Check 'settlement figures come from the Settlement group members' ($report["2026-08/settlement/$mae/BRL"] -eq [decimal]'70.50' -and $report["2026-08/settlement/$mae/GBP"] -eq [decimal]'15.25' -and $report["2026-09/settlement/$mae/BRL"] -eq [decimal]'137.88' -and $report["2026-09/settlement/$mae/GBP"] -eq [decimal]'7.94') "$($report["2026-08/settlement/$mae/BRL"]) $($report["2026-08/settlement/$mae/GBP"]) $($report["2026-09/settlement/$mae/BRL"]) $($report["2026-09/settlement/$mae/GBP"])"
    Check 'settlement figures are exact decimals' ($report["2026-09/settlement/$mae/GBP"] -is [decimal]) ''
    Check 'a Settlement group adds no template group figures' (-not ($report.Keys | Where-Object { $_ -like '*/templateGroups/Settlement/*' })) ''

    $withoutGroup = $responses.Clone()
    $withoutGroup['accounts/groups/by-template/2026/9'] = $responses['accounts/groups/by-template/2026/9'].Replace($settlementGroup, '')
    $withoutGroupFetch = { param($path) ConvertFrom-ParityJson $withoutGroup[$path] }
    $withoutGroupReport = Get-ParityReport -Fetch $withoutGroupFetch -First '2026-09' -Last '2026-09' -CurrentMonth '2026-09'
    Check 'no settlement figures without a Settlement group' (-not (& $settlementKeys $withoutGroupReport)) ((& $settlementKeys $withoutGroupReport) -join ', ')

    $readPaths = [System.Collections.Generic.List[string]]::new()
    $recordingFetch = { param($path) $readPaths.Add($path); & $fetch $path }
    $null = Get-ParityReport -Fetch $recordingFetch -First '2026-08' -Last '2026-09' -CurrentMonth '2026-09'
    Check 'the report reads no retired route' (-not ($readPaths | Where-Object { $_ -like 'controle-mae/*' -or $_ -like 'reserve/*' -or $_ -eq 'banks' -or $_ -like 'banks/*' -or ($_ -like 'expenses/month/*' -and $_ -notlike 'expenses/month/*/*/category-totals') -or $_ -like 'card-statements/*' })) ($readPaths -join ', ')
    Check 'the report reads each month''s transaction rows once and no account''s own rows' (@($readPaths | Where-Object { $_ -eq 'accounts/transactions/2026/9' }).Count -eq 1 -and -not ($readPaths | Where-Object { $_ -match '^accounts/[^/]+/transactions/' })) ($readPaths -join ', ')
    Check 'the report reads the Pots tab of the account that replaces the Reserva page only' (-not ($readPaths | Where-Object { $_ -like "accounts/$otherPotsId/*" }) -and $readPaths -contains "accounts/$reservaId/pots/2026/9") ($readPaths -join ', ')

    Check 'the Mae totals come from the current balances of the account that replaces the page' ($report['current/maeAccount/totalBrl'] -eq [decimal]'167.88' -and $report['current/maeAccount/totalGbp'] -eq [decimal]'7.94') "$($report['current/maeAccount/totalBrl']) $($report['current/maeAccount/totalGbp'])"
    Check 'no Mae totals when no account replaces the page' (-not ($filled.Keys | Where-Object { $_ -like 'current/maeAccount/*' })) ''

    $dataPath = Join-Path $work 'data-cashflow.json'
    Set-Content -LiteralPath $dataPath -Value '{ "Expenses": [ { "Date": "2019-03-04T00:00:00", "ChargeDate": null, "InvoiceDate": "2027-02-01T00:00:00" } ], "Incomes": [ { "Date": "2026-10-14T00:00:00" } ], "SettlementTransactions": [ { "Date": "2018-05-06" } ], "InvestmentSnapshots": [ { "Year": 2026, "Month": 12 } ], "CardStatements": [], "Version": 1 }'
    $range = Get-ParityMonthRange $dataPath ([datetime]'2026-10-18')
    $laterNow = Get-ParityMonthRange $dataPath ([datetime]'2027-05-02')
    Check 'month range spans first record, settlement entries included, to later of now and last record' ($range.First -eq '2018-05' -and $range.Last -eq '2027-02' -and $range.CurrentMonth -eq '2026-10' -and $laterNow.Last -eq '2027-05') "$($range.First)..$($range.Last) / $($laterNow.Last)"

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
    Check 'compare step fails and lists the changed figure' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape('2026-09 / account / Barclays / balance | 130.83 | 131.83') -and $output -match 'Parity FAILED: 1 difference\b' -and (Test-Path (Join-Path $runFolder 'differences.txt'))) $output

    $output = & pwsh -NoProfile -File $entry -Step All -RepoRoot $emptyRoot 2>&1 | Out-String
    Check 'entry script stops before anything runs when there is no source' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape($expectedMissingSource) -and $output -notmatch 'Work folder') $output

    git init --quiet $repoRoot
    $copyPath = Join-Path $repoRoot 'data/copy.json'
    $output = & pwsh -NoProfile -File $entry -Step Baseline -LiveCopy $copyPath -RepoRoot $repoRoot -BeforePort $freePort 2>&1 | Out-String
    Check 'entry script refuses a missing baseline tag before anything runs' ($LASTEXITCODE -eq 1 -and $output -match [regex]::Escape('Tag p60-deployed-f13 not found. Create it on main before the first run.') -and $output -notmatch 'Work folder') $output

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
