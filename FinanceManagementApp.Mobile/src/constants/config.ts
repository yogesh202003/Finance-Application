export const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://10.0.2.2:5280';

export const COLORS = {
  primary: '#0B3A53',
  primaryDark: '#072636',
  accent: '#1F8A70',
  background: '#F3F6F8',
  surface: '#FFFFFF',
  text: '#12263A',
  textMuted: '#5B6B7C',
  border: '#D7E0E7',
  danger: '#C0392B',
  warning: '#B7791F',
  success: '#1F8A70',
  pendingBg: '#FFF7E6',
  approvedBg: '#E8F7F2',
  rejectedBg: '#FDECEC',
};

export const SPACING = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
};

export const ROLES = {
  ADMIN: 'ADMIN',
  EMPLOYEE: 'EMPLOYEE',
} as const;
