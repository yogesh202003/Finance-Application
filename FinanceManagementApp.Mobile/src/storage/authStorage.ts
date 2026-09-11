import AsyncStorage from '@react-native-async-storage/async-storage';
import type { AuthSession } from '../types';

const AUTH_KEY = 'financeapp.auth';

export async function saveSession(session: AuthSession): Promise<void> {
  await AsyncStorage.setItem(AUTH_KEY, JSON.stringify(session));
}

export async function getSession(): Promise<AuthSession | null> {
  const raw = await AsyncStorage.getItem(AUTH_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    return null;
  }
}

export async function clearSession(): Promise<void> {
  await AsyncStorage.removeItem(AUTH_KEY);
}

export function isTokenExpired(expiration: string): boolean {
  const exp = new Date(expiration).getTime();
  return Number.isNaN(exp) || exp <= Date.now();
}
