import type {
  Beneficiary,
  CreateBeneficiaryRequest,
  CreatePaymentRequest,
  CreateQuoteRequest,
  DailyExchangeRates,
  Payment,
  PaymentStatus,
  Quote,
  Session,
} from "./types";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  (import.meta.env.DEV ? "http://localhost:5231" : "");

async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers);

  headers.set("Accept", "application/json");

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}.`;

    try {
      const errorBody = (await response.json()) as {
        message?: string;
        title?: string;
      };

      message = errorBody.message ?? errorBody.title ?? message;
    } catch {
      // Keep the generic message if the response has no JSON body.
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function getSession(signal: AbortSignal) {
  return apiRequest<{ session: Session | null; portfolioEnabled: boolean }>("/api/session", { signal });
}

export function enterDemo(role: Session["role"]) {
  return apiRequest<Session>("/api/session/demo", {
    method: "POST",
    body: JSON.stringify({ role }),
  });
}

export function getDailyExchangeRates(signal: AbortSignal) {
  return apiRequest<DailyExchangeRates>("/api/exchange-rates", { signal });
}

export function signIn(username: string, password: string) {
  return apiRequest<Session>("/api/session/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}

export function signOut() {
  return apiRequest<void>("/api/session/logout", {
    method: "POST",
    body: "{}",
  });
}

export function getBeneficiaries(
  signal?: AbortSignal,
): Promise<Beneficiary[]> {
  return apiRequest<Beneficiary[]>("/api/beneficiaries", {
    signal,
  });
}

export function createBeneficiary(
  beneficiary: CreateBeneficiaryRequest,
): Promise<Beneficiary> {
  return apiRequest<Beneficiary>("/api/beneficiaries", {
    method: "POST",
    body: JSON.stringify(beneficiary),
  });
}

export function createQuote(
  quoteRequest: CreateQuoteRequest,
): Promise<Quote> {
  return apiRequest<Quote>("/api/quotes", {
    method: "POST",
    body: JSON.stringify(quoteRequest),
  });
}

export function getPayments(
  signal?: AbortSignal,
): Promise<Payment[]> {
  return apiRequest<Payment[]>("/api/payments", {
    signal,
  });
}

export function createPayment(
  paymentRequest: CreatePaymentRequest,
): Promise<Payment> {
  return apiRequest<Payment>("/api/payments", {
    method: "POST",
    body: JSON.stringify(paymentRequest),
  });
}

export function updatePaymentStatus(
  paymentId: number,
  status: PaymentStatus,
): Promise<Payment> {
  return apiRequest<Payment>(`/api/payments/${paymentId}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status }),
  });
}
