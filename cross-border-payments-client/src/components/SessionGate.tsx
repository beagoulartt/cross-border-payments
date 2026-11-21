import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { enterDemo, getSession, signIn } from "../api";
import type { Session } from "../types";
import { PortfolioLanding } from "./PortfolioLanding";

interface SessionGateProps {
  children: (
    session: Session,
    onSignedOut: () => void,
    onSessionChanged: (session: Session) => void,
  ) => ReactNode;
}

export function SessionGate({ children }: SessionGateProps) {
  const [session, setSession] = useState<Session | null>(null);
  const [initializing, setInitializing] = useState(true);
  const [portfolioEnabled, setPortfolioEnabled] = useState(false);
  const [signingIn, setSigningIn] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function restoreSession() {
      try {
        const result = await getSession(controller.signal);
        if (!controller.signal.aborted) {
          setSession(result.session);
          setPortfolioEnabled(result.portfolioEnabled);
        }
      } catch {
        if (!controller.signal.aborted) setError("Unable to check your session.");
      } finally {
        if (!controller.signal.aborted) setInitializing(false);
      }
    }

    void restoreSession();
    return () => controller.abort();
  }, []);

  async function handleSignIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await startSession(() => signIn(String(form.get("username")), String(form.get("password"))));
  }

  async function startSession(request: () => Promise<Session>) {
    setSigningIn(true);
    setError("");
    try {
      setSession(await request());
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unable to sign in.");
    } finally {
      setSigningIn(false);
    }
  }

  if (initializing) {
    return <main className="app-shell loading">Checking session...</main>;
  }

  if (session) {
    return children(session, () => setSession(null), setSession);
  }

  if (portfolioEnabled) {
    return (
      <PortfolioLanding
        entering={signingIn}
        error={error}
        onEnter={(role) => void startSession(() => enterDemo(role))}
      />
    );
  }

  return (
    <PortfolioLanding entering={signingIn} error={error}>
      <form className="login-form portfolio-sign-in" onSubmit={handleSignIn}>
        <label>
          Username
          <input name="username" autoComplete="username" required />
        </label>
        <label>
          Password
          <input name="password" type="password" autoComplete="current-password" required />
        </label>
        <button className="login-submit" type="submit" disabled={signingIn}>
          {signingIn ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </PortfolioLanding>
  );
}
