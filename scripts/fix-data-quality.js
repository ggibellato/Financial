const fs = require('fs');
const path = 'data/data-investment.json';
const data = JSON.parse(fs.readFileSync(path, 'utf-8'));

const PORTFOLIO_TO_CLASS = {
  ETF: 'ETF',
  REIT: 'RealEstate',
  Share: 'Equity',
  Shares: 'Equity',
  FII: 'RealEstate',
  Fundo: 'Fund',
  'PIE Shares experiment': 'Equity',
};

// Verified REITs/bond ETFs sitting in a Shares-named portfolio; override the portfolio-name default.
const ASSET_NAME_CLASS_OVERRIDES = {
  'AGREE REALTY CORPORATION (ADC)': 'RealEstate',
  'STAG INDUSTRIAL, INC. (STAG)': 'RealEstate',
  'PIMCO Short-Term High IE00B7N3YW49': 'ETF',
  'PIMCO Short-Term High IE00BYXVWC37': 'ETF',
};

function findAsset(brokerName, portfolioName, assetName) {
  for (const scopeName of ['ActiveBrokers', 'HistoricBrokers']) {
    for (const broker of data[scopeName] ?? []) {
      if (broker.Name !== brokerName) continue;
      for (const portfolio of broker.Portfolios ?? []) {
        if (portfolio.Name !== portfolioName) continue;
        const asset = portfolio.Assets.find(a => a.Name === assetName);
        if (asset) return asset;
      }
    }
  }
  return null;
}

let unclassifiedFixed = 0;

for (const scopeName of ['ActiveBrokers', 'HistoricBrokers']) {
  for (const broker of data[scopeName] ?? []) {
    for (const portfolio of broker.Portfolios ?? []) {
      const targetClass = PORTFOLIO_TO_CLASS[portfolio.Name];
      if (!targetClass) continue;

      for (const asset of portfolio.Assets ?? []) {
        if (asset.Class === 'Unknown') {
          asset.Class = ASSET_NAME_CLASS_OVERRIDES[asset.Name] ?? targetClass;
          unclassifiedFixed++;
        }
      }
    }
  }
}

// Sells recorded at a 2-decimal-rounded quantity instead of the fund's full-precision
// quota total, making the sell look like it exceeds what was held by a fraction of a unit.
const SALE_QUANTITY_CORRECTIONS = [
  { broker: 'XPI', portfolio: 'Fundo', asset: 'Brasil Capital 30 Advisory FIC FIA', transactionId: '0deaead8-8106-4c52-8ca2-9bb0883597fe', from: 1904.12, to: 1904.11545741 },
  { broker: 'XPI', portfolio: 'Fundo', asset: 'Hix Capital Institucional FIC FIA', transactionId: '5b4ae8c3-c958-4392-bb47-1f4af4ff0df2', from: 2128.38, to: 2128.37599271 },
];

let saleQuantityFixed = 0;

for (const correction of SALE_QUANTITY_CORRECTIONS) {
  const asset = findAsset(correction.broker, correction.portfolio, correction.asset);
  const transaction = asset?.Transactions.find(t => t.Id === correction.transactionId);
  if (transaction && transaction.Quantity === correction.from) {
    transaction.Quantity = correction.to;
    saleQuantityFixed++;
  }
}

// The original spreadsheet import recorded this Buy with a £0.00 unit price by mistake;
// IEM.L's LSE closing price on the purchase date (536p) fills in the real cost basis.
const impaxEnviro = findAsset('FreeTrade', 'Share', 'ImpaxEnviro');
let missingBuyInserted = 0;
if (impaxEnviro && !impaxEnviro.Transactions.some(t => t.Type === 'Buy')) {
  impaxEnviro.Transactions.push({
    Id: 'a1c9e6f4-6d2e-4b1a-9c3e-2f7b8d5a4e10',
    Date: '2021-09-08T00:00:00',
    Type: 'Buy',
    Quantity: 1,
    UnitPrice: 5.36,
    Fees: 0,
    Withheld: 0,
  });
  missingBuyInserted++;
}

// PATL11 was incorporated into HGLG11 (a fund merger, not a market sale): TransferOut has no
// cash effect, so it zeroes the quantity without inventing a sale price/gain; the value carried
// here is informational only (ties to what the merger proceeds bought in HGLG11-2).
const patl11 = findAsset('XPI', 'FII', 'PATL11');
let conversionFixed = 0;
if (patl11 && !patl11.Transactions.some(t => t.Type === 'TransferOut')) {
  patl11.Transactions.push({
    Id: 'f3b2c8a0-9e4d-4f1a-8c6b-2d5e7a9f1b3c',
    Date: '2026-06-05T00:00:00',
    Type: 'TransferOut',
    Quantity: 28,
    UnitPrice: 54.125,
    Fees: 0,
    Withheld: 0,
  });
  conversionFixed++;
}

fs.writeFileSync(path, JSON.stringify(data));
console.log(`Done. Fixed ${unclassifiedFixed} unclassified holding(s), ${saleQuantityFixed} rounded sale quantity(ies), inserted ${missingBuyInserted} missing buy transaction(s), ${conversionFixed} fund-conversion(s).`);
