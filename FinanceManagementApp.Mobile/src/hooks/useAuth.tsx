import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { login as loginApi } from '../api/services';
import { setUnauthorizedHandler } from '../api/client';
import { clearSession, getSession, isTokenExpired, saveSession } from '../storage/authStorage';
import type { AuthSession, Role } from '../types';

interface AuthContextValue {
  session: AuthSession | null;
  bootstrapping: boolean;
  login: (usernameOrEmail: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  role: Role | null;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [bootstrapping, setBootstrapping] = useState(true);

  const logout = useCallback(async () => {
    await clearSession();
    setSession(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setSession(null);
    });
    (async () => {
      const stored = await getSession();
      if (stored && !isTokenExpired(stored.expiration)) {
        setSession(stored);
      } else if (stored) {
        await clearSession();
      }
      setBootstrapping(false);
    })();
  }, []);

  const login = useCallback(async (usernameOrEmail: string, password: string) => {
    const next = await loginApi(usernameOrEmail, password);
    await saveSession(next);
    setSession(next);
  }, []);

  const value = useMemo(
    () => ({
      session,
      bootstrapping,
      login,
      logout,
      role: session?.role ?? null,
    }),
    [session, bootstrapping, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
