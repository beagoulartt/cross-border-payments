import { useState } from "react";
import type { DailyExchangeRates } from "../types";

const rateFormat = new Intl.NumberFormat("en-CA", { maximumFractionDigits: 6 });
const amountFormat = new Intl.NumberFormat("en-CA", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function CurrencyConverter({ snapshot }: { snapshot: DailyExchangeRates }) {
  const [amount, setAmount] = useState("1000");
  const [from, setFrom] = useState(snapshot.baseCurrency);
  const [to, setTo] = useState("BRL");
  const rates = { [snapshot.baseCurrency]: 1, ...snapshot.rates };
  const currencies = Object.keys(rates);
  const rate = rates[to] / rates[from];
  const convertedAmount = Number(amount) * rate;
  const validAmount = amount !== "" && convertedAmount >= 0 && Number.isFinite(convertedAmount);

  return (
    <section className="portfolio-converter" aria-labelledby="converter-title">
      <h3 id="converter-title">Currency converter</h3>
      <div className="portfolio-converter-fields">
        <label className="portfolio-amount">
          Amount
          <input type="number" min="0" step="0.01" inputMode="decimal"
            value={amount} onChange={(event) => setAmount(event.target.value)} />
        </label>
        <label>
          From
          <select value={from} onChange={(event) => setFrom(event.target.value)}>
            {currencies.map((currency) => <option key={currency}>{currency}</option>)}
          </select>
        </label>
        <button type="button" className="portfolio-swap" aria-label="Swap currencies"
          title="Swap currencies" onClick={() => { setFrom(to); setTo(from); }}>
          <span aria-hidden="true">⇄</span>
        </button>
        <label>
          To
          <select value={to} onChange={(event) => setTo(event.target.value)}>
            {currencies.map((currency) => <option key={currency}>{currency}</option>)}
          </select>
        </label>
      </div>
      <div className="portfolio-conversion">
        <span>Estimated value</span>
        <output aria-live="polite">
          {validAmount ? `${amountFormat.format(convertedAmount)} ${to}` : "Enter a valid amount"}
        </output>
        <small>1 {from} = {rateFormat.format(rate)} {to} · Before fees</small>
      </div>
    </section>
  );
}
