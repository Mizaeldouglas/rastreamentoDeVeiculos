import axios from "axios";
import { API_BASE_URL } from "./config";
import type { AuthResponseDto } from "../types/auth";

const TOKEN_KEY = "rastreador_token";
const SESSION_KEY = "rastreador_session";

let unauthorizedHandler: (() => void) | null = null;

export function onUnauthorized(handler: () => void) {
  unauthorizedHandler = handler;
}

export function triggerUnauthorized() {
  clearSession();
  unauthorizedHandler?.();
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getSession(): AuthResponseDto | null {
  const raw = localStorage.getItem(SESSION_KEY);
  return raw ? JSON.parse(raw) : null;
}

function saveSession(data: AuthResponseDto) {
  localStorage.setItem(TOKEN_KEY, data.token);
  localStorage.setItem(SESSION_KEY, JSON.stringify(data));
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(SESSION_KEY);
}

export function isAuthenticated(): boolean {
  return !!getToken();
}

const authClient = axios.create({ baseURL: API_BASE_URL });

export async function login(email: string, password: string): Promise<AuthResponseDto> {
  const { data } = await authClient.post<AuthResponseDto>("/api/auth/login", { email, password });
  saveSession(data);
  return data;
}

export async function register(
  companyName: string,
  email: string,
  password: string
): Promise<AuthResponseDto> {
  const { data } = await authClient.post<AuthResponseDto>("/api/auth/register", {
    companyName,
    email,
    password,
  });
  saveSession(data);
  return data;
}

export function logout() {
  clearSession();
}
