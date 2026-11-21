import { useState, type FormEvent } from "react";
import { createPayment, createQuote } from "../api";
import type { Beneficiary, Payment, Quote } from "../types";

const supportedCurrencies = ["CAD", "USD", "EUR", "GBP", "BRL"];

interface CurrencyQuoteFormProps {
  beneficiaries: Beneficiary[];
  onPaymentCreated: (payment: Payment) => void;
}

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat("en-CA", {
    style: "currency",
    currency,
  }).format(amount);
}

export function CurrencyQuoteForm({
  beneficiaries,
  onPaymentCreated,
}: CurrencyQuoteFormProps) {
  const [sourceAmount, setSourceAmount] = useState("1000");
  const [sourceCurrency, setSourceCurrency] = useState("CAD");
  const [destinationCurrency, setDestinationCurrency] =
    useState("USD");

  const [quote, setQuote] = useState<Quote | null>(null);
  const [beneficiaryId, setBeneficiaryId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isCreatingPayment, setIsCreatingPayment] = useState(false);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const eligibleBeneficiaries = beneficiaries.filter(
    (beneficiary) =>
      beneficiary.isActive &&
      beneficiary.currency.toUpperCase() ===
        destinationCurrency.toUpperCase(),
  );

  function clearQuote() {
    setQuote(null);
    setBeneficiaryId("");
    setSuccessMessage("");
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const amount = Number(sourceAmount);

    if (!Number.isFinite(amount) || amount <= 0) {
      setError("Enter an amount greater than zero.");
      return;
    }

    try {
      setIsLoading(true);
      setError("");
      setSuccessMessage("");
      setQuote(null);

      const createdQuote = await createQuote({
        sourceAmount: amount,
        sourceCurrency,
        destinationCurrency,
      });

      setQuote(createdQuote);

      const firstEligibleBeneficiary = beneficiaries.find(
        (beneficiary) =>
          beneficiary.isActive &&
          beneficiary.currency.toUpperCase() ===
            createdQuote.destinationCurrency.toUpperCase(),
      );

      setBeneficiaryId(
        firstEligibleBeneficiary
          ? String(firstEligibleBeneficiary.id)
          : "",
      );
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to create the currency quote.",
      );
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreatePayment() {
    if (!quote || !beneficiaryId) {
      setError("Select a beneficiary before creating the payment.");
      return;
    }

    if (new Date(quote.expiresAtUtc).getTime() <= Date.now()) {
      setError("This quote has expired. Request a new quote.");
      setQuote(null);
      return;
    }

    try {
      setIsCreatingPayment(true);
      setError("");
      setSuccessMessage("");

      const payment = await createPayment({
        beneficiaryId: Number(beneficiaryId),
        quoteId: quote.quoteId,
      });

      onPaymentCreated(payment);
      setQuote(null);
      setBeneficiaryId("");
      setSuccessMessage(`Payment #${payment.id} created successfully.`);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to create the payment.",
      );
    } finally {
      setIsCreatingPayment(false);
    }
  }

  return (
    <section className="panel">
      <div className="panel-heading">
        <div>
          <h2>Get a currency quote</h2>
          <p>
            Review the exchange rate, transaction fee, and recipient
            amount.
          </p>
        </div>
      </div>

      <form className="quote-form" onSubmit={handleSubmit}>
        <label>
          You send
          <input
            type="number"
            min="0.01"
            step="0.01"
            value={sourceAmount}
            disabled={isLoading || isCreatingPayment}
            onChange={(event) => {
              setSourceAmount(event.target.value);
              clearQuote();
            }}
            required
          />
        </label>

        <label>
          Source currency
          <select
            value={sourceCurrency}
            disabled={isLoading || isCreatingPayment}
            onChange={(event) => {
              setSourceCurrency(event.target.value);
              clearQuote();
            }}
          >
            {supportedCurrencies.map((currency) => (
              <option value={currency} key={currency}>
                {currency}
              </option>
            ))}
          </select>
        </label>

        <label>
          Destination currency
          <select
            value={destinationCurrency}
            disabled={isLoading || isCreatingPayment}
            onChange={(event) => {
              setDestinationCurrency(event.target.value);
              clearQuote();
            }}
          >
            {supportedCurrencies.map((currency) => (
              <option value={currency} key={currency}>
                {currency}
              </option>
            ))}
          </select>
        </label>

        <div className="form-actions">
          <button type="submit" disabled={isLoading || isCreatingPayment}>
            {isLoading ? "Calculating..." : "Get quote"}
          </button>
        </div>

        {error && <p className="form-error">{error}</p>}

        {successMessage && (
          <p className="form-success">{successMessage}</p>
        )}
      </form>

      {quote && (
        <>
          <div className="quote-result">
            <div>
              <span>You send</span>
              <strong>
                {formatMoney(
                  quote.sourceAmount,
                  quote.sourceCurrency,
                )}
              </strong>
            </div>

            <div>
              <span>Exchange rate</span>
              <strong>
                1 {quote.sourceCurrency} ={" "}
                {quote.exchangeRate.toFixed(6)}{" "}
                {quote.destinationCurrency}
              </strong>
            </div>

            <div>
              <span>Transaction fee</span>
              <strong>
                {formatMoney(quote.fee, quote.sourceCurrency)}
              </strong>
            </div>

            <div className="quote-destination">
              <span>Recipient receives</span>
              <strong>
                {formatMoney(
                  quote.destinationAmount,
                  quote.destinationCurrency,
                )}
              </strong>
            </div>

            <p className="quote-expiration">
              Rate from {quote.rateProvider}, effective{" "}
              {new Date(
                `${quote.rateEffectiveDate}T00:00:00`,
              ).toLocaleDateString("en-CA", {
                year: "numeric",
                month: "short",
                day: "numeric",
              })}
              . Quote valid until{" "}
              {new Date(quote.expiresAtUtc).toLocaleTimeString(
                "en-CA",
                {
                  hour: "numeric",
                  minute: "2-digit",
                },
              )}
            </p>
          </div>

          <div className="payment-confirmation">
            <label>
              Beneficiary receiving {quote.destinationCurrency}
              <select
                value={beneficiaryId}
                onChange={(event) =>
                  setBeneficiaryId(event.target.value)
                }
                disabled={eligibleBeneficiaries.length === 0 || isCreatingPayment}
              >
                {eligibleBeneficiaries.length === 0 ? (
                  <option value="">
                    No matching active beneficiaries
                  </option>
                ) : (
                  <>
                    <option value="">Select a beneficiary</option>
                    {eligibleBeneficiaries.map((beneficiary) => (
                      <option
                        value={beneficiary.id}
                        key={beneficiary.id}
                      >
                        {beneficiary.name} — {beneficiary.bankName}{" "}
                        {beneficiary.accountNumberMasked}
                      </option>
                    ))}
                  </>
                )}
              </select>
            </label>

            <button
              type="button"
              onClick={() => void handleCreatePayment()}
              disabled={!beneficiaryId || isCreatingPayment}
            >
              {isCreatingPayment
                ? "Creating payment..."
                : "Create payment"}
            </button>

            {eligibleBeneficiaries.length === 0 && (
              <p>
                Add an active {quote.destinationCurrency} beneficiary
                before creating this payment.
              </p>
            )}
          </div>
        </>
      )}
    </section>
  );
}
