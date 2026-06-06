# Google Finance HTML Selector Guide

This document explains how to update the Google Finance parser when Google changes their HTML structure.

## Overview

The `GoogleFinance.cs` parser uses a **multi-strategy approach** with fallbacks to handle HTML structure changes gracefully. Each data extraction method tries multiple strategies in order of preference.

**All selectors are centralized in `GoogleFinanceSelectors.cs`** - update that file when HTML changes!

## Current HTML Structure (as of last update)

For the URL: `https://www.google.com/finance/quote/BBAS3:BVMF`

The main container structure is:
```html
<div class="KycIzb">
    <div class="YTGvuc">
        <div class="gO24Ff">Banco do Brasil SA</div>  <!-- Asset Name -->
    </div>
    <div class="LhDNu">
        <div class="zhtAvb">
            <div class="ujg0He">
                <div class="N6SYTe">
                    <span jsname="Pdsbrc" class="">
                        <span>R$19.17</span>  <!-- Price -->
                    </span>
                </div>
                <!-- ... price change info ... -->
            </div>
            <div class="jZZ2de">Jun 5, 10:44:17 PM UTC-3 · BRL</div>  <!-- Timestamp -->
        </div>
    </div>
</div>
```

## Quick Update Guide

### Step 1: Identify What Changed

1. Open the Google Finance page in a browser (e.g., `https://www.google.com/finance/quote/BBAS3:BVMF`)
2. Right-click on the element that's failing → "Inspect Element"
3. Identify the new class name, jsname, or container structure

### Step 2: Update GoogleFinanceSelectors.cs

Open `Integrations\WebPageParser\GoogleFinanceSelectors.cs` and update the appropriate constant:

#### For Main Container Changes
```csharp
public static class MainContainer
{
    public const string PrimaryClass = "KycIzb";  // ← Update this
    public const string PriceJsName = "Pdsbrc";   // ← Or this
}
```

#### For Asset Name Changes
```csharp
public static class AssetName
{
    public const string PrimaryClass = "gO24Ff";      // ← Update this
    public const string ContainerClass = "YTGvuc";    // ← Or this
}
```

#### For Price Changes
```csharp
public static class Price
{
    public const string PrimaryJsName = "Pdsbrc";        // ← Update this (most stable)
    public const string ContainerClass = "N6SYTe";       // ← Or this
    public const string PricePattern = @"^[R$£€¥]*\s*\d+[.,]\d+\s*(GBX)?$";  // ← Regex pattern
}
```

#### For Timestamp Changes
```csharp
public static class Timestamp
{
    public const string PrimaryClass = "jZZ2de";  // ← Update this
    public const string DatePattern = @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},\s+\d{1,2}:\d{2}:\d{2}\s+(?:AM|PM)";
}
```

### Step 3: Test Your Changes

```bash
# Build the solution
dotnet build

# Run the tests
dotnet test --filter "FullyQualifiedName~GoogleFinance"

# Or run all infrastructure tests
dotnet test Tests\Financial.Infrastructure.Tests
```

### Step 4: Manual Testing (Recommended)

Test with real URLs from different exchanges to ensure the parser works across regions:
- **Brazil (BVMF):** `https://www.google.com/finance/quote/BBAS3:BVMF`
- **US (NYSE):** `https://www.google.com/finance/quote/AAPL:NASDAQ`
- **UK (LSE):** `https://www.google.com/finance/quote/BP:LON`

## Detailed Strategy Breakdown

### 1. Finding the Main Container (`GetMainData`)

**Current strategies (in order):**
1. **Primary:** Look for `div[@class='KycIzb']` (from `MainContainer.PrimaryClass`)
2. **Fallback 1:** Find price element by jsname and traverse up to find container with asset name
3. **Fallback 2:** Use `//main` tag as last resort

**When to update:**
- If the main container class changes, update `MainContainer.PrimaryClass`
- The jsname approach is usually more stable than class names

### 2. Extracting Asset Name (`ReadAssetName`)

**Current strategies:**
1. **Primary:** `div[@class='gO24Ff']` (from `AssetName.PrimaryClass`)
2. **Fallback 1:** Find container then get first child div
3. **Fallback 2:** Pattern match for substantial text (5-100 chars)

**When to update:**
- Inspect the asset name element and update `AssetName.PrimaryClass`

### 3. Extracting Price (`ReadPriceText`)

**Current strategies:**
1. **Primary:** `span[@jsname='Pdsbrc']` (from `Price.PrimaryJsName`) - **Most stable!**
2. **Fallback 1:** Find price container class
3. **Fallback 2:** Regex pattern matching

**When to update:**
- The `jsname` attribute is Google's internal identifier and rarely changes
- If it does change, update `Price.PrimaryJsName`
- The regex fallback handles most currency formats automatically

### 4. Extracting Timestamp (`ReadAsOfText`)

**Current strategies:**
1. **Primary:** `div[@class='jZZ2de']` (from `Timestamp.PrimaryClass`)
2. **Fallback 1:** Regex pattern for date/time formats
3. **Fallback 2:** Return empty (triggers `DateTimeOffset.Now`)

**When to update:**
- Update `Timestamp.PrimaryClass` if the container class changes

## XPath Quick Reference

| Selector | Description |
|----------|-------------|
| `.//div[@class='Name']` | Find descendant div with exact class |
| `.//span[@jsname='Name']` | Find span with jsname attribute |
| `//main` | Find main tag anywhere in document |
| `.//div[contains(@class, 'Partial')]` | Find div containing class substring |
| `descendant::span` | Find all span descendants |

## Strategy Philosophy

Each extraction method follows this pattern:
1. **Specific → Generic:** Try most specific selector first, fall back to more generic
2. **Attributes → Structure:** Prefer unique attributes (jsname, id) over class/structure
3. **Pattern matching last:** Use regex/text patterns as last resort before error

This approach maximizes stability while making updates straightforward.

## Troubleshooting

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| "main data node not found" | Main container class changed | Update `MainContainer.PrimaryClass` |
| "Asset name not found" | Asset name element class changed | Update `AssetName.PrimaryClass` |
| "Price not found" | Price element changed | Check jsname first, update `Price.PrimaryJsName` |
| Wrong values extracted | HTML structure changed | Check parent/child relationships |

## Advanced: If Multiple Selectors Changed

If Google performs a major redesign and multiple selectors need updating:

1. **Use browser DevTools:** Open the page, right-click → Inspect
2. **Find the data:** Locate the three key pieces of data (name, price, timestamp)
3. **Identify patterns:** Look for unique attributes (jsname is best, then id, then class)
4. **Update selectors:** Update `GoogleFinanceSelectors.cs` with new values
5. **Test incrementally:** Build and test after each selector update

## Files Modified

When updating selectors, you typically only need to modify:
- ✅ `Integrations\WebPageParser\GoogleFinanceSelectors.cs` - **Primary update location**
- ✅ This documentation file (optional, to record changes)

You should **NOT** need to modify:
- ❌ `Integrations\WebPageParser\GoogleFinance.cs` - Logic stays the same
- ❌ `Integrations\WebPageParser\GoogleFinanceParsing.cs` - Parsing logic unchanged
