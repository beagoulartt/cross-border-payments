import type { Beneficiary, Payment } from "./types";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5231";

async function apiRequest<T>(
  path: string,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    signal,
    headers: {
      Accept: "application/json",
    },
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}.`;

    try {
      const errorBody = (await response.json()) as {
        message?: string;
      };

      if (errorBody.message) {
        message = errorBody.message;
      }
    } catch {
      // Keep the generic message if the response has no JSON body.
    }

    throw new Error(message);
  }

  return (await response.json()) as T;
}

export function getBeneficiaries(
  signal?: AbortSignal,
): Promise<Beneficiary[]> {
  return apiRequest<Beneficiary[]>("/api/beneficiaries", signal);
}

export function getPayments(signal?: AbortSignal): Promise<Payment[]> {
  return apiRequest<Payment[]>("/api/payments", signal);
}