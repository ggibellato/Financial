namespace Financial.Integrations.WebPageParser;

/// <summary>
/// Configuration for Google Finance HTML selectors.
/// Update these selectors when Google changes their page structure.
/// 
/// HOW TO FIND NEW SELECTORS:
/// 1. Open Google Finance page in browser
/// 2. Right-click the element (price, name, etc.) → Inspect
/// 3. Look for attributes in this order of preference:
///    - jsname (most stable, e.g., jsname="Pdsbrc")
///    - id (stable but rarely used)
///    - class (changes occasionally, e.g., class="N6SYTe")
/// 4. Update the constants below
/// 5. Run: GoogleFinanceVerificationTests or GoogleFinanceVerifier.VerifyMultipleUrls()
/// </summary>
internal class GoogleFinanceSelectors
{
    /// <summary>
    /// Main container that holds all financial data.
    /// Current: KycIzb div wraps all financial information
    /// </summary>
    public static class MainContainer
    {
        public const string PrimaryClass = "KycIzb";

        public const string PriceJsName = "Pdsbrc";

        /// <summary>
        /// Alternative jsname values to try if primary fails.
        /// Add new values here when Google changes the jsname.
        /// </summary>
        public static readonly string[] AlternativePriceJsNames = Array.Empty<string>();
    }

    /// <summary>
    /// Asset name selectors.
    /// Current: gO24Ff class contains the company/asset name
    /// </summary>
    public static class AssetName
    {
        public const string PrimaryClass = "gO24Ff";

        public const string ContainerClass = "YTGvuc";
    }

    /// <summary>
    /// Price value selectors.
    /// Current: Pdsbrc jsname is most stable
    /// </summary>
    public static class Price
    {
        /// <summary>
        /// Primary jsname for the price span (MOST STABLE).
        /// </summary>
        public const string PrimaryJsName = "Pdsbrc";

        /// <summary>
        /// Alternative jsname values to try if primary fails.
        /// Add new values here when Google changes the jsname.
        /// </summary>
        public static readonly string[] AlternativeJsNames = Array.Empty<string>();

        public const string ContainerClass = "N6SYTe";

        /// <summary>
        /// Regex pattern to match price values as last resort.
        /// Matches: $123.45, R$19.17, £45.67, 123.45 GBX, etc.
        /// </summary>
        public const string PricePattern = @"^[R$£€¥]*\s*\d+[.,]\d+\s*(GBX)?$";
    }

    /// <summary>
    /// Timestamp selectors.
    /// Current: jZZ2de class contains the date/time
    /// </summary>
    public static class Timestamp
    {
        public const string PrimaryClass = "jZZ2de";

        /// <summary>
        /// Regex pattern to match date/time text.
        /// Matches: "Jun 5, 10:44:17 PM", "Dec 31, 1:23:45 AM", etc.
        /// </summary>
        public const string DatePattern = @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},\s+\d{1,2}:\d{2}:\d{2}\s+(?:AM|PM)";
    }
}
