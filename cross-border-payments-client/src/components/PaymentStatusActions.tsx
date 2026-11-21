import { useState } from "react";
import { updatePaymentStatus } from "../api";
import type { Payment, PaymentStatus } from "../types";

interface PaymentStatusActionsProps {
  payment: Payment;
  onUpdated: (payment: Payment) => void;
  automaticCompletion: boolean;
}

const nextStatuses: Record<PaymentStatus, PaymentStatus[]> = {
  Draft: ["Submitted"],
  Submitted: ["PendingApproval", "Rejected"],
  PendingApproval: ["Processing", "Rejected"],
  Processing: ["Completed", "Failed"],
  Completed: [],
  Rejected: [],
  Failed: [],
};

const actionLabels: Partial<Record<PaymentStatus, string>> = {
  Submitted: "Submit for review",
  PendingApproval: "Request approval",
  Processing: "Approve",
  Rejected: "Reject",
  Completed: "Simulate completion",
  Failed: "Simulate failure",
};

export function PaymentStatusActions({
  payment,
  onUpdated,
  automaticCompletion,
}: PaymentStatusActionsProps) {
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState("");

  const availableStatuses = nextStatuses[payment.status];

  async function handleStatusChange(status: PaymentStatus) {
    try {
      setIsUpdating(true);
      setError("");

      const updatedPayment = await updatePaymentStatus(
        payment.id,
        status,
      );

      onUpdated(updatedPayment);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to update payment status.",
      );
    } finally {
      setIsUpdating(false);
    }
  }

  if (automaticCompletion && payment.status === "Processing") {
    return <span className="no-actions">Completing automatically</span>;
  }

  if (availableStatuses.length === 0) {
    return <span className="no-actions">No actions</span>;
  }

  return (
    <div className="status-actions">
      <div className="status-action-buttons">
        {availableStatuses.map((status) => (
          <button
            type="button"
            key={status}
            disabled={isUpdating}
            onClick={() => void handleStatusChange(status)}
            className={
              status === "Rejected" || status === "Failed"
                ? "status-action-danger"
                : ""
            }
          >
            {isUpdating ? "Updating..." : actionLabels[status]}
          </button>
        ))}
      </div>

      {error && (
        <span className="status-action-error">{error}</span>
      )}
    </div>
  );
}
