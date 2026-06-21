import { useState } from "react";
import { login, register } from "../services/auth";
import type { AuthResponseDto } from "../types/auth";
import "./LoginPage.css";

interface Props {
  onAuthenticated: (session: AuthResponseDto) => void;
}

export function LoginPage({ onAuthenticated }: Props) {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [companyName, setCompanyName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const session =
        mode === "login"
          ? await login(email, password)
          : await register(companyName, email, password);
      onAuthenticated(session);
    } catch (err) {
      console.error(err);
      setError(
        mode === "login"
          ? "E-mail ou senha inválidos."
          : "Não foi possível criar a conta. Verifique os dados e tente novamente."
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="login-shell">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>Rastreador Veicular</h1>
        <p className="subtitle">
          {mode === "login" ? "Entre com sua conta" : "Cadastre sua empresa"}
        </p>

        {error && <div className="alert alert-error">{error}</div>}

        {mode === "register" && (
          <label className="field">
            <span className="field-label">Nome da empresa</span>
            <input value={companyName} onChange={(e) => setCompanyName(e.target.value)} required />
          </label>
        )}

        <label className="field">
          <span className="field-label">E-mail</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>

        <label className="field">
          <span className="field-label">Senha</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            minLength={6}
            required
          />
        </label>

        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? "Aguarde..." : mode === "login" ? "Entrar" : "Criar conta"}
        </button>

        <button
          type="button"
          className="link-button"
          onClick={() => {
            setMode((m) => (m === "login" ? "register" : "login"));
            setError(null);
          }}
        >
          {mode === "login" ? "Não tem conta? Cadastre sua empresa" : "Já tem conta? Entrar"}
        </button>
      </form>
    </div>
  );
}
