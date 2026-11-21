import { PaymentStatusActions } from "./components/PaymentStatusActions";
import { CurrencyQuoteForm } from "./components/CurrencyQuoteForm";
import { useEffect, useMemo, useState } from "react";
import { enterDemo, getBeneficiaries, getPayments, signOut } from "./api";
import { AddBeneficiaryForm } from "./components/AddBeneficiaryForm";
import type { Beneficiary, Payment, PaymentStatus, Session } from "./types";
import { SessionGate } from "./components/SessionGate";
import { DailyRatesPanel } from "./components/DailyRatesPanel";
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

function PaymentWorkspace({
  session,
  onSignedOut,
  onSessionChanged,
}: {
  session: Session;
  onSignedOut: () => void;
  onSessionChanged: (session: Session) => void;
}) {
  const isOperations = session.role === "Administrator";
  const [beneficiaries, setBeneficiaries] = useState<Beneficiary[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [signingOut, setSigningOut] = useState(false);
  const [switchingRole, setSwitchingRole] = useState(false);
  const hasProcessingPayment = payments.some((payment) => payment.status === "Processing");

  function handleBeneficiaryCreated(
    beneficiary: Beneficiary,
  ) {
    setBeneficiaries((currentBeneficiaries) => [
      beneficiary,
      ...currentBeneficiaries,
    ]);
  }

  function handlePaymentUpdated(updatedPayment: Payment) {
    setPayments((currentPayments) =>
      currentPayments.map((payment) =>
        payment.id === updatedPayment.id
          ? updatedPayment
          : payment,
      ),
    );
  }

  function handlePaymentCreated(payment: Payment) {
    setPayments((currentPayments) => [payment, ...currentPayments]);
  }

  async function handleSignOut() {
    setSigningOut(true);
    try {
      await signOut();
      onSignedOut();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unable to sign out.");
      setSigningOut(false);
    }
  }

  async function handleSwitchRole() {
    setSwitchingRole(true);
    setError("");
    try {
      onSessionChanged(await enterDemo(isOperations ? "User" : "Administrator"));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unable to switch roles.");
      setSwitchingRole(false);
    }
  }

  useEffect(() => {
    const controller = new AbortController();

    async function loadDashboard() {
      try {
        setIsLoading(true);
        setError("");

        const [beneficiaryData, paymentData] = await Promise.all([
          isOperations ? Promise.resolve([]) : getBeneficiaries(controller.signal),
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
  }, [isOperations]);

  useEffect(() => {
    if (!session.isDemo || !hasProcessingPayment) return;

    const controller = new AbortController();
    const refresh = window.setInterval(async () => {
      try {
        const latest = await getPayments(controller.signal);
        if (!controller.signal.aborted) {
          setPayments((current) => current.map((payment) =>
            payment.status === "Processing"
              ? latest.find((candidate) => candidate.id === payment.id) ?? payment
              : payment,
          ));
        }
      } catch {
        // A temporary connection issue is retried on the next refresh.
      }
    }, 3000);

    return () => {
      window.clearInterval(refresh);
      controller.abort();
    };
  }, [session.isDemo, hasProcessingPayment]);

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
          <h1>{isOperations ? "Payment Operations" : "Payment Dashboard"}</h1>
          <p className="subtitle">
            {isOperations
              ? "Review payments and simulate transfer outcomes."
              : "Manage beneficiaries, send payments, and track their status."}
          </p>
        </div>

        <div className="profile">
          <div>
            <strong>{session.username}</strong>
            <span>{session.role}</span>
          </div>
          {session.isDemo && (
            <button type="button" disabled={switchingRole || signingOut}
              onClick={() => void handleSwitchRole()}>
              {switchingRole ? "Switching..." : isOperations ? "Switch to user" : "Switch to administrator"}
            </button>
          )}
          <button type="button" disabled={signingOut || switchingRole} onClick={() => void handleSignOut()}>
            {signingOut ? "Signing out..." : "Sign out"}
          </button>
        </div>
      </header>

      {error && <div className="error-message">{error}</div>}

      {!isOperations && (
        <>
          <DailyRatesPanel />

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

          <AddBeneficiaryForm
            onCreated={handleBeneficiaryCreated}
          />

          <CurrencyQuoteForm
            beneficiaries={beneficiaries}
            onPaymentCreated={handlePaymentCreated}
          />
        </>
      )}

      {isLoading ? (
        <div className="loading">Loading payment data...</div>
      ) : (
        <>
          <section className="panel">
            <div className="panel-heading">
              <div>
                <h2>{isOperations ? "Payment workflow" : "Recent payments"}</h2>
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
                    {isOperations && <th>Actions</th>}
                  </tr>
                </thead>

                <tbody>
                  {payments.length === 0 ? (
                    <tr>
                      <td colSpan={isOperations ? 7 : 6} className="empty-state">
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
                        {isOperations && (
                          <td>
                            <PaymentStatusActions
                              payment={payment}
                              onUpdated={handlePaymentUpdated}
                              automaticCompletion={session.isDemo}
                            />
                          </td>
                        )}
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>

          {!isOperations && (
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
          )}
        </>
      )}
    </main>
  );
}

export default function App() {
  return (
    <SessionGate>
      {(session, onSignedOut, onSessionChanged) => (
        <PaymentWorkspace key={session.role} session={session}
          onSignedOut={onSignedOut} onSessionChanged={onSessionChanged} />
      )}
    </SessionGate>
  );
}
