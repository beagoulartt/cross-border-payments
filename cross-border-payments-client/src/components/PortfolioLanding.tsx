import type { ReactNode } from "react";
import type { Session } from "../types";
import { DailyRatesPanel } from "./DailyRatesPanel";
import "./PortfolioLanding.css";

interface PortfolioLandingProps {
  entering: boolean;
  error: string;
  onEnter?: (role: Session["role"]) => void;
  children?: ReactNode;
}

export function PortfolioLanding({ entering, error, onEnter, children }: PortfolioLandingProps) {
  return (
    <div className="portfolio-page">
      <header className="portfolio-header portfolio-container">
        <span className="portfolio-brand">
          <span className="portfolio-brand-mark" aria-hidden="true">↔</span>
          Cross-Border Payments
        </span>
        {onEnter && <a href="#demo-workflow">How it works</a>}
      </header>

      <main className="portfolio-container">
        <div className="portfolio-hero">
          <section className="portfolio-intro" aria-labelledby="portfolio-title">
            {onEnter && <p className="portfolio-kicker">Interactive portfolio demo</p>}
            <h1 id="portfolio-title">International payments</h1>
            <p className="portfolio-lede">
              {onEnter
                ? "Submit a transfer as a user, then switch to Operations to approve it and follow the simulated payout."
                : "Sign in to request, approve, or track simulated international transfers."}
            </p>
            {onEnter ? (
              <>
                <div className="portfolio-actions">
                  <button type="button" disabled={entering} onClick={() => onEnter("User")}>
                    Try as user <span aria-hidden="true">→</span>
                  </button>
                  <button type="button" disabled={entering} onClick={() => onEnter("Administrator")}>
                    Try as administrator
                  </button>
                </div>
                {entering && <p className="portfolio-note" role="status">Opening demo…</p>}
                <p className="portfolio-note">No account required. Use fictional details for all transfers.</p>
              </>
            ) : children}
            {error && <p className="portfolio-error" role="alert">{error}</p>}
          </section>

          <div className="portfolio-market">
            <DailyRatesPanel showConverter />
          </div>
        </div>

        {onEnter && <section id="demo-workflow" className="portfolio-workflow" aria-labelledby="workflow-title">
          <h2 id="workflow-title">How the demo works</h2>
          <ol>
            <li><strong>Request a transfer</strong><span>Choose a sample recipient and confirm a quote.</span></li>
            <li><strong>Review in Operations</strong><span>Switch roles to approve or reject the request.</span></li>
            <li><strong>Track the payout</strong><span>Watch the status update automatically after approval.</span></li>
          </ol>
        </section>}
      </main>
    </div>
  );
}
