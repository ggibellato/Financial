const fs = require('fs');
const path = 'data/data-cashflow.json';
const data = JSON.parse(fs.readFileSync(path, 'utf-8'));

const expense = data.Expenses.find(e => e.Id === '9bf99e70-1445-4e78-9322-17859ffaf0a8');
if (!expense) { throw new Error('Expense not found'); }
expense.CountsAsTithe = false;

data.TitheCarryForwardEffectiveFrom = '2026-08-01';

fs.writeFileSync(path, JSON.stringify(data));
console.log('Done.');