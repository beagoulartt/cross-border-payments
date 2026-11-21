import { useState, type FormEvent } from "react";
import { createBeneficiary } from "../api";
import type {
  Beneficiary,
  CreateBeneficiaryRequest,
} from "../types";

interface AddBeneficiaryFormProps {
  onCreated: (beneficiary: Beneficiary) => void;
}

const initialForm: CreateBeneficiaryRequest = {
  name: "",
  country: "",
  currency: "",
  bankName: "",
  accountNumber: "",
};

export function AddBeneficiaryForm({
  onCreated,
}: AddBeneficiaryFormProps) {
  const [form, setForm] =
    useState<CreateBeneficiaryRequest>(initialForm);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function updateField(
    field: keyof CreateBeneficiaryRequest,
    value: string,
  ) {
    setForm((currentForm) => ({
      ...currentForm,
      [field]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      setIsSubmitting(true);
      setError("");
      setSuccessMessage("");

      const createdBeneficiary = await createBeneficiary({
        ...form,
        currency: form.currency.toUpperCase(),
      });

      onCreated(createdBeneficiary);
      setForm(initialForm);
      setSuccessMessage("Beneficiary added successfully.");
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to add beneficiary.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="panel">
      <div className="panel-heading">
        <div>
          <h2>Add beneficiary</h2>
          <p>Create a new recipient for international payments.</p>
        </div>
      </div>

      <form className="beneficiary-form" onSubmit={handleSubmit}>
        <label>
          Beneficiary name
          <input
            type="text"
            value={form.name}
            onChange={(event) =>
              updateField("name", event.target.value)
            }
            required
            maxLength={100}
          />
        </label>

        <label>
          Country
          <input
            type="text"
            value={form.country}
            onChange={(event) =>
              updateField("country", event.target.value)
            }
            required
            maxLength={100}
          />
        </label>

        <label>
          Currency
          <input
            type="text"
            value={form.currency}
            onChange={(event) =>
              updateField("currency", event.target.value)
            }
            placeholder="USD"
            required
            minLength={3}
            maxLength={3}
          />
        </label>

        <label>
          Bank name
          <input
            type="text"
            value={form.bankName}
            onChange={(event) =>
              updateField("bankName", event.target.value)
            }
            required
            maxLength={100}
          />
        </label>

        <label>
          Account number
          <input
            type="text"
            value={form.accountNumber}
            onChange={(event) =>
              updateField("accountNumber", event.target.value)
            }
            required
            maxLength={50}
          />
        </label>

        <div className="form-actions">
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Adding..." : "Add beneficiary"}
          </button>
        </div>

        {error && <p className="form-error">{error}</p>}

        {successMessage && (
          <p className="form-success">{successMessage}</p>
        )}
      </form>
    </section>
  );
}
