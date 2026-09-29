const fs = require('fs');
const path = process.argv[2] ?? 'data/data-cashflow.json';
const data = JSON.parse(fs.readFileSync(path, 'utf-8'));

function roundHalfAwayFromZero(value) {
  return Math.sign(value) * Math.round(Math.abs(value) * 100 + Number.EPSILON) / 100;
}

let movementsFixed = 0;

for (const movement of data.ReserveMovements ?? []) {
  const rounded = roundHalfAwayFromZero(movement.Amount);
  if (rounded !== movement.Amount) {
    movement.Amount = rounded;
    movementsFixed++;
  }
}

const backupPath = `${path}.bak-${new Date().toISOString().replace(/[:.]/g, '-')}`;
fs.copyFileSync(path, backupPath);
fs.writeFileSync(path, JSON.stringify(data));
console.log(`Done. Rounded ${movementsFixed} reserve movement amount(s) to 2 decimal places in ${path}. Backup: ${backupPath}`);
