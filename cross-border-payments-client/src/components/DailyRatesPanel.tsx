import { useEffect, useState } from "react";
import { getDailyExchangeRates } from "../api";
import type { DailyExchangeRates } from "../types";
import { CurrencyConverter } from "./CurrencyConverter";

const rateFormat = new Intl.NumberFormat("en-CA", {
  minimumFractionDigits: 4,
  maximumFractionDigits: 6,
});

export function DailyRatesPanel({ showConverter = false }: { showConverter?: boolean }) {
  const [rates, setRates] = useState<DailyExchangeRates | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  useEffect(() => {
    const controller = new AbortController();

    async function loadRates() {
      try {
        const latest = await getDailyExchangeRates(controller.signal);
        if (!controller.signal.aborted) {
          setRates(latest);
          setUnavailable(false);
        }
      } catch {
        if (!controller.signal.aborted) setUnavailable(true);
      }
    }

    void loadRates();
    const refresh = window.setInterval(() => void loadRates(), 15 * 60 * 1000);
    return () => {
      window.clearInterval(refresh);
      controller.abort();
    };
  }, []);

  return (
    <section className="panel" aria-labelledby="daily-rates-heading">
      <div className="panel-heading">
        <div>
          <h2 id="daily-rates-heading">Daily exchange rates</h2>
          {rates && (
            <p>
              Published{" "}
              <time dateTime={rates.effectiveDate}>
                {new Date(`${rates.effectiveDate}T00:00:00`).toLocaleDateString(
                  "en-CA",
                  { dateStyle: "long" },
                )}
              </time>
              {" · "}
              <a href={rates.sourceUrl} target="_blank" rel="noreferrer">
                {rates.provider}
              </a>
            </p>
          )}
        </div>
      </div>

      {rates && showConverter && <CurrencyConverter snapshot={rates} />}

      {rates && (
        <ul className="daily-rate-grid">
          {Object.entries(rates.rates).map(([currency, value]) => (
            <li key={currency}>
              <span>1 {rates.baseCurrency} in {currency}</span>
              <strong>{rateFormat.format(value)} {currency}</strong>
            </li>
          ))}
        </ul>
      )}

      {unavailable ? (
        <p className="form-error" role="status">
          {rates
            ? "Unable to refresh rates. Showing the last available rates."
            : "Daily exchange rates are temporarily unavailable."}
        </p>
      ) : !rates && <p role="status">Loading daily exchange rates…</p>}
    </section>
  );
}
