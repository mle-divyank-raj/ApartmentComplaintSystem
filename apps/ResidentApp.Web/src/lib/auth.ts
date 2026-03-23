import { getStoredToken, clearToken } from "@acls/sdk";
import type { AuthTokenResponse } from "@acls/api-contracts";

const SESSION_KEY = "acls_session";

export interface StoredSession {
  userId: number;
  role: string;
  propertyId: number;
  expiresAt: string;
}

export function storeSession(auth: AuthTokenResponse): void {
  const session: StoredSession = {
    userId: auth.userId,
    role: auth.role,
    propertyId: auth.propertyId,
    expiresAt: auth.expiresAt,
  };
  window.localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function getSession(): StoredSession | null {
  if (typeof window === "undefined") return null;
  const raw = window.localStorage.getItem(SESSION_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    return null;
  }
}

export function isAuthenticated(): boolean {
  const token = getStoredToken();
  const session = getSession();
  if (!token || !session) return false;
  return new Date(session.expiresAt) > new Date();
}

export function signOut(): void {
  clearToken();
  window.localStorage.removeItem(SESSION_KEY);
}
