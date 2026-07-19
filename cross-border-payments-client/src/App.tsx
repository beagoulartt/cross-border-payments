import { useEffect, useMemo, useState } from "react";
import { getBeneficiaries, getPayments } from "./api";
import type { Beneficiary, Payment, PaymentStatus } from "./types";
import "./App.css";

function formatDate(date: string): string {
  return new Intl.DateTimeFormat("en-CA", {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(date));
}

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat("en-CA", {
    style: "currency",
    currency,
  }).format(amount);
}

function statusClass(status: PaymentStatus): string {
  return status.toLowerCase();
}

function App() {
  const [beneficiaries, setBeneficiaries] = useState<Beneficiary[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadDashboard() {
      try {
        setIsLoading(true);
        setError("");

        const [beneficiaryData, paymentData] = await Promise.all([
          getBeneficiaries(controller.signal),
          getPayments(controller.signal),
        ]);

        setBeneficiaries(beneficiaryData);
        setPayments(paymentData);
      } catch (requestError) {
        if (
          requestError instanceof DOMException &&
          requestError.name === "AbortError"
        ) {
          return;
        }

        setError(
          requestError instanceof Error
            ? requestError.message
            : "Unable to load the dashboard.",
        );
      } finally {
        setIsLoading(false);
      }
    }

    void loadDashboard();

    return () => {
      controller.abort();
    };
  }, []);

  const completedPayments = useMemo(
    () => payments.filter((payment) => payment.status === "Completed").length,
    [payments],
  );

  const activePayments = useMemo(
    () =>
      payments.filter((payment) =>
        ["Submitted", "PendingApproval", "Processing"].includes(
          payment.status,
        ),
      ).length,
    [payments],
  );

  return (
    <main className="app-shell">
      <header className="page-header">
        <div>
          <p className="eyebrow">Cross-border payments</p>
          <h1>Payment Dashboard</h1>
          <p className="subtitle">
            Manage beneficiaries and monitor international transactions.
          </p>
        </div>

        <div className="profile">
          <div className="avatar">BG</div>
          <div>
            <strong>Beatriz Goulart</strong>
            <span>Administrator</span>
          </div>
        </div>
      </header>

      {error && <div className="error-message">{error}</div>}

      <section className="summary-grid">
        <article className="summary-card">
          <span>Total payments</span>
          <strong>{payments.length}</strong>
        </article>

        <article className="summary-card">
          <span>Active payments</span>
          <strong>{activePayments}</strong>
        </article>

        <article className="summary-card">
          <span>Completed</span>
          <strong>{completedPayments}</strong>
        </article>

        <article className="summary-card">
          <span>Beneficiaries</span>
          <strong>{beneficiaries.length}</strong>
        </article>
      </section>

      {isLoading ? (
        <div className="loading">Loading payment data...</div>
      ) : (
        <>
          <section className="panel">
            <div className="panel-heading">
              <div>
                <h2>Recent payments</h2>
                <p>Transactions currently stored in SQL Server.</p>
              </div>
            </div>

            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Beneficiary</th>
                    <th>You send</th>
                    <th>Recipient receives</th>
                    <th>Status</th>
                    <th>Date</th>
                  </tr>
                </thead>

                <tbody>
                  {payments.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="empty-state">
                        No payments have been created.
                      </td>
                    </tr>
                  ) : (
                    payments.map((payment) => (
                      <tr key={payment.id}>
                        <td>#{payment.id}</td>
                        <td>{payment.beneficiaryName}</td>
                        <td>
                          {formatMoney(
                            payment.sourceAmount,
                            payment.sourceCurrency,
                          )}
                        </td>
                        <td>
                          {formatMoney(
                            payment.destinationAmount,
                            payment.destinationCurrency,
                          )}
                        </td>
                        <td>
                          <span
                            className={`status ${statusClass(payment.status)}`}
                          >
                            {payment.status}
                          </span>
                        </td>
                        <td>{formatDate(payment.createdAtUtc)}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>

          <section className="panel">
            <div className="panel-heading">
              <div>
                <h2>Beneficiaries</h2>
                <p>Approved recipients available for payments.</p>
              </div>
            </div>

            <div className="beneficiary-grid">
              {beneficiaries.length === 0 ? (
                <p className="empty-state">No beneficiaries found.</p>
              ) : (
                beneficiaries.map((beneficiary) => (
                  <article
                    className="beneficiary-card"
                    key={beneficiary.id}
                  >
                    <div className="beneficiary-initial">
                      {beneficiary.name.charAt(0).toUpperCase()}
                    </div>

                    <div>
                      <h3>{beneficiary.name}</h3>
                      <p>
                        {beneficiary.bankName} ·{" "}
                        {beneficiary.accountNumberMasked}
                      </p>
                      <span>
                        {beneficiary.country} · {beneficiary.currency}
                      </span>
                    </div>
                  </article>
                ))
              )}
            </div>
          </section>
        </>
      )}
    </main>
  );
}

export default App;