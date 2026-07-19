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