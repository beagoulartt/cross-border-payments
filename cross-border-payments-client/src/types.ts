export interface Session {
  username: string;
  role: "Administrator" | "User";
  isDemo: boolean;
}

export interface DailyExchangeRates {
  baseCurrency: string;
  rates: Record<string, number>;
  effectiveDate: string;
  provider: string;
  sourceUrl: string;
}

export interface Beneficiary {
  id: number;
  name: string;
  country: string;
  currency: string;
  bankName: string;
  accountNumberMasked: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateBeneficiaryRequest {
  name: string;
  country: string;
  currency: string;
  bankName: string;
  accountNumber: string;
}

export type PaymentStatus =
  | "Draft"
  | "Submitted"
  | "PendingApproval"
  | "Processing"
  | "Completed"
  | "Rejected"
  | "Failed";

export interface Payment {
  id: number;
  beneficiaryId: number;
  beneficiaryName: string;
  sourceAmount: number;
  sourceCurrency: string;
  destinationAmount: number;
  destinationCurrency: string;
  exchangeRate: number;
  fee: number;
  status: PaymentStatus;
  createdAtUtc: string;
}

export interface CreateQuoteRequest {
  sourceAmount: number;
  sourceCurrency: string;
  destinationCurrency: string;
}

export interface Quote {
  quoteId: string;
  sourceAmount: number;
  sourceCurrency: string;
  exchangeRate: number;
  fee: number;
  destinationAmount: number;
  destinationCurrency: string;
  expiresAtUtc: string;
  rateProvider: string;
  rateEffectiveDate: string;
}

export interface CreatePaymentRequest {
  beneficiaryId: number;
  quoteId: string;
}
