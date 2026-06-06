# How to Verify Google Finance Selectors

When you suspect Google has changed their HTML structure, use these steps to verify and update the selectors.

## Quick Verification (Manual Test)

### Option 1: Run the Verifier Utility

1. Open **Test Explorer** in Visual Studio
2. Locate `GoogleFinanceVerificationTests` class
3. Remove the `Skip` parameter from any test:
   ```csharp
   // Change from:
   [Fact(Skip = "Manual verification test - requires internet connection")]

   // To:
   [Fact]
   ```
4. Run the test
5. Check the output window for details on which strategies worked

### Option 2: Run via PowerShell

Create a simple console app or use LINQPad with this code:

```csharp
using Financial.Infrastructure.Integrations;

GoogleFinanceVerifier.VerifyMultipleUrls();
```

This will test multiple URLs and show:
- ✓ SUCCESS or ✗ FAILED for each URL
- Which strategy was used for each element
- The actual values extracted

## What to Look For

The verifier tests these URLs by default:
- `https://www.google.com/finance/quote/BBAS3:BVMF` (Banco do Brasil)
- `https://www.google.com/finance/quote/KLBN4:BVMF` (Klabin PN)
- `https://www.google.com/finance/quote/KLBN11:BVMF` (Klabin Unit)
- `https://www.google.com/finance/quote/AAPL:NASDAQ` (Apple)

### Expected Output (when working):

```
Testing: BBAS3:BVMF
URL: https://www.google.com/finance/quote/BBAS3:BVMF
✓ SUCCESS
  Asset Name: Banco do Brasil SA
  Price: R$19.17
  Timestamp: Jun 5, 10:44:17 PM UTC-3 · BRL
  Strategies Used:
    - Main Container: Strategy 1: PrimaryClass (KycIzb)
    - Asset Name: Strategy 1: PrimaryClass (gO24Ff)
    - Price: Strategy 1: PrimaryJsName (Pdsbrc)
    - Timestamp: Strategy 1: PrimaryClass (jZZ2de)
```

### If Something Fails:

Example of failure output:
```
Testing: BBAS3:BVMF
✗ FAILED
  Error: Price not found. The page structure may have changed.
  Strategies Used:
    - Main Container: Strategy 1: PrimaryClass (KycIzb)
    - Asset Name: Strategy 1: PrimaryClass (gO24Ff)
    - Price: FAILED: No strategy worked
    - Timestamp: Strategy 1: PrimaryClass (jZZ2de)
```

This tells you that the **Price** selector needs updating.

## How to Update Selectors

When a selector fails, follow these steps:

### Step 1: Inspect the Page

1. Open the failing URL in your browser
2. Right-click on the element (price, name, etc.) → **Inspect Element**
3. Look for these attributes in order:
   - `jsname` (most stable) - e.g., `jsname="NewName"`
   - `id` (stable but rare)
   - `class` (changes more often) - e.g., `class="SomeNewClass"`

### Step 2: Update GoogleFinanceSelectors.cs

Open `Integrations\WebPageParser\GoogleFinanceSelectors.cs` and update:

#### If jsname changed:
```csharp
public const string PrimaryJsName = "NewJsName"; // Update to new value
```

#### If you want to support BOTH old and new (during transition):
```csharp
public const string PrimaryJsName = "Pdsbrc"; // Keep old
public static readonly string[] AlternativeJsNames = new[] { "NewJsName" }; // Add new
```

#### If class changed:
```csharp
public const string PrimaryClass = "NewClassName"; // Update to new value
```

### Step 3: Test Again

1. Run the verifier again
2. Check that all tests pass
3. Run the full test suite: `dotnet test`

## Current Selectors (as of last update)

| Element | Primary Selector | Type | Value |
|---------|------------------|------|-------|
| Main Container | Class | `div` | `KycIzb` |
| Asset Name | Class | `div` | `gO24Ff` |
| Price | **jsname** | `span` | `Pdsbrc` ⭐ |
| Timestamp | Class | `div` | `jZZ2de` |

⭐ = Most stable selector (jsname rarely changes)

## Understanding Strategies

Each element uses multiple fallback strategies:

### Main Container
1. **Strategy 1:** Look for `div[@class='KycIzb']`
2. **Strategy 2:** Find price by jsname, traverse up to container
3. **Strategy 3:** Use `<main>` tag

### Asset Name
1. **Strategy 1:** Look for `div[@class='gO24Ff']`
2. **Strategy 2:** Find container `div[@class='YTGvuc']`, get first child
3. **Strategy 3:** Pattern match for text (5-100 chars)

### Price (Most Important)
1. **Strategy 1a:** Look for `span[@jsname='Pdsbrc']` ⭐
2. **Strategy 1b:** Try alternative jsname values
3. **Strategy 2:** Look for `div[@class='N6SYTe']`
4. **Strategy 3:** Regex pattern match for price format

### Timestamp
1. **Strategy 1:** Look for `div[@class='jZZ2de']`
2. **Strategy 2:** Regex pattern for date/time
3. **Strategy 3:** Return empty (fallback to `DateTimeOffset.Now`)

## Troubleshooting

### "Price not found" error
- Most likely: jsname changed
- Inspect the price element in browser
- Update `GoogleFinanceSelectors.Price.PrimaryJsName`

### "Asset name not found" error
- Class name changed
- Update `GoogleFinanceSelectors.AssetName.PrimaryClass`

### "Main data node not found" error
- Container structure changed significantly
- Update `GoogleFinanceSelectors.MainContainer.PrimaryClass`
- Or add alternative jsname to traverse up from price

### All strategies fail
- Major redesign
- Check if URL structure changed (`/finance/quote/` vs `/finance/beta/quote/`)
- Update multiple selectors
- Consider opening an issue if this becomes frequent

## Beta URL Notice

The URLs you mentioned use `/finance/beta/quote/` instead of `/finance/quote/`. Both should work with the same selectors since they render the same HTML structure. If the beta version has different selectors, you may need to:

1. Detect if URL contains `/beta/` 
2. Use different selector sets for beta vs. production

Let me know if you need help with this!
