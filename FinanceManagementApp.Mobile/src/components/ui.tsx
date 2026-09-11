import React from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
  ViewStyle,
} from 'react-native';
import { COLORS, SPACING } from '../constants/config';

export function Button({
  title,
  onPress,
  loading,
  variant = 'primary',
  disabled,
  style,
}: {
  title: string;
  onPress: () => void;
  loading?: boolean;
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  disabled?: boolean;
  style?: ViewStyle;
}) {
  const bg =
    variant === 'primary'
      ? COLORS.primary
      : variant === 'secondary'
        ? COLORS.accent
        : variant === 'danger'
          ? COLORS.danger
          : 'transparent';
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled || loading}
      style={[
        styles.button,
        { backgroundColor: bg, opacity: disabled || loading ? 0.6 : 1 },
        variant === 'ghost' && { borderWidth: 1, borderColor: COLORS.border },
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={variant === 'ghost' ? COLORS.primary : '#fff'} />
      ) : (
        <Text style={[styles.buttonText, variant === 'ghost' && { color: COLORS.primary }]}>{title}</Text>
      )}
    </Pressable>
  );
}

export function Input({
  label,
  value,
  onChangeText,
  placeholder,
  secureTextEntry,
  error,
  keyboardType,
  multiline,
}: {
  label: string;
  value: string;
  onChangeText: (v: string) => void;
  placeholder?: string;
  secureTextEntry?: boolean;
  error?: string;
  keyboardType?: 'default' | 'email-address' | 'numeric' | 'decimal-pad';
  multiline?: boolean;
}) {
  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        secureTextEntry={secureTextEntry}
        keyboardType={keyboardType}
        multiline={multiline}
        placeholderTextColor={COLORS.textMuted}
        style={[styles.input, multiline && { minHeight: 90, textAlignVertical: 'top' }, error ? styles.inputError : null]}
      />
      {!!error && <Text style={styles.error}>{error}</Text>}
    </View>
  );
}

export function Card({ children, style }: { children: React.ReactNode; style?: ViewStyle }) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function StatusBadge({ status }: { status: string }) {
  const upper = status.toUpperCase();
  const bg =
    upper === 'APPROVED' || upper === 'ACTIVE'
      ? COLORS.approvedBg
      : upper === 'REJECTED' || upper === 'INACTIVE'
        ? COLORS.rejectedBg
        : COLORS.pendingBg;
  const color =
    upper === 'APPROVED' || upper === 'ACTIVE'
      ? COLORS.success
      : upper === 'REJECTED' || upper === 'INACTIVE'
        ? COLORS.danger
        : COLORS.warning;
  const icon = upper === 'APPROVED' || upper === 'ACTIVE' ? '✓' : upper === 'REJECTED' || upper === 'INACTIVE' ? '✕' : '•';
  return (
    <View style={[styles.badge, { backgroundColor: bg }]}>
      <Text style={[styles.badgeText, { color }]}>
        {icon} {upper}
      </Text>
    </View>
  );
}

export function Header({ title, subtitle, right }: { title: string; subtitle?: string; right?: React.ReactNode }) {
  return (
    <View style={styles.header}>
      <View style={{ flex: 1 }}>
        <Text style={styles.headerTitle}>{title}</Text>
        {!!subtitle && <Text style={styles.headerSubtitle}>{subtitle}</Text>}
      </View>
      {right}
    </View>
  );
}

export function SearchBar({ value, onChangeText, placeholder }: { value: string; onChangeText: (v: string) => void; placeholder?: string }) {
  return (
    <TextInput
      value={value}
      onChangeText={onChangeText}
      placeholder={placeholder ?? 'Search...'}
      placeholderTextColor={COLORS.textMuted}
      style={styles.search}
    />
  );
}

export function LoadingView({ message = 'Loading...' }: { message?: string }) {
  return (
    <View style={styles.center}>
      <ActivityIndicator size="large" color={COLORS.primary} />
      <Text style={styles.centerText}>{message}</Text>
    </View>
  );
}

export function EmptyView({ message = 'No data found.' }: { message?: string }) {
  return (
    <View style={styles.center}>
      <Text style={styles.centerText}>{message}</Text>
    </View>
  );
}

export function ErrorView({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <View style={styles.center}>
      <Text style={[styles.centerText, { color: COLORS.danger }]}>{message}</Text>
      {onRetry && <Button title="Retry" onPress={onRetry} style={{ marginTop: SPACING.md, minWidth: 140 }} />}
    </View>
  );
}

export function StatTile({ label, value }: { label: string; value: string | number }) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

export function ExpenseCard({
  expense,
  onPress,
}: {
  expense: { id: number; categoryName: string; amount: number; expenseDate: string; status: string; employeeName?: string; description: string };
  onPress?: () => void;
}) {
  return (
    <Pressable onPress={onPress} style={styles.listCard}>
      <View style={styles.rowBetween}>
        <Text style={styles.cardTitle}>EXP{String(expense.id).padStart(3, '0')}</Text>
        <StatusBadge status={expense.status} />
      </View>
      <Text style={styles.cardMeta}>{expense.categoryName} · ${expense.amount.toFixed(2)}</Text>
      {!!expense.employeeName && <Text style={styles.cardMeta}>{expense.employeeName}</Text>}
      <Text numberOfLines={2} style={styles.cardDesc}>{expense.description}</Text>
      <Text style={styles.cardMeta}>{new Date(expense.expenseDate).toLocaleDateString()}</Text>
    </Pressable>
  );
}

export function EmployeeCard({
  employee,
  onPress,
}: {
  employee: { employeeCode: string; firstName: string; lastName: string; departmentName: string; designation: string; status: string; email: string };
  onPress?: () => void;
}) {
  return (
    <Pressable onPress={onPress} style={styles.listCard}>
      <View style={styles.rowBetween}>
        <Text style={styles.cardTitle}>{employee.employeeCode}</Text>
        <StatusBadge status={employee.status} />
      </View>
      <Text style={styles.cardMeta}>{employee.firstName} {employee.lastName}</Text>
      <Text style={styles.cardMeta}>{employee.departmentName} · {employee.designation}</Text>
      <Text style={styles.cardDesc}>{employee.email}</Text>
    </Pressable>
  );
}

export function NotificationItemView({
  item,
  onPress,
}: {
  item: { title: string; message: string; isRead: boolean; createdAt: string };
  onPress?: () => void;
}) {
  return (
    <Pressable onPress={onPress} style={[styles.listCard, !item.isRead && { borderLeftWidth: 4, borderLeftColor: COLORS.accent }]}>
      <Text style={styles.cardTitle}>{item.title}</Text>
      <Text style={styles.cardDesc}>{item.message}</Text>
      <Text style={styles.cardMeta}>{new Date(item.createdAt).toLocaleString()}</Text>
    </Pressable>
  );
}

export function TransactionCard({
  tx,
}: {
  tx: { referenceNumber: string; amount: number; transactionType: string; description: string; transactionDate: string; employeeName?: string };
}) {
  return (
    <View style={styles.listCard}>
      <View style={styles.rowBetween}>
        <Text style={styles.cardTitle}>{tx.referenceNumber}</Text>
        <Text style={{ color: COLORS.danger, fontWeight: '700' }}>
          {tx.transactionType} ${tx.amount.toFixed(2)}
        </Text>
      </View>
      {!!tx.employeeName && <Text style={styles.cardMeta}>{tx.employeeName}</Text>}
      <Text style={styles.cardDesc}>{tx.description}</Text>
      <Text style={styles.cardMeta}>{new Date(tx.transactionDate).toLocaleString()}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  button: {
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: 'center',
    justifyContent: 'center',
  },
  buttonText: { color: '#fff', fontWeight: '700', fontSize: 15 },
  field: { marginBottom: SPACING.md },
  label: { color: COLORS.text, marginBottom: 6, fontWeight: '600' },
  input: {
    backgroundColor: COLORS.surface,
    borderWidth: 1,
    borderColor: COLORS.border,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    color: COLORS.text,
  },
  inputError: { borderColor: COLORS.danger },
  error: { color: COLORS.danger, marginTop: 4, fontSize: 12 },
  card: {
    backgroundColor: COLORS.surface,
    borderRadius: 14,
    padding: SPACING.md,
    borderWidth: 1,
    borderColor: COLORS.border,
  },
  badge: { paddingHorizontal: 10, paddingVertical: 4, borderRadius: 999 },
  badgeText: { fontSize: 12, fontWeight: '700' },
  header: { flexDirection: 'row', alignItems: 'center', marginBottom: SPACING.md },
  headerTitle: { fontSize: 24, fontWeight: '800', color: COLORS.text },
  headerSubtitle: { color: COLORS.textMuted, marginTop: 2 },
  search: {
    backgroundColor: COLORS.surface,
    borderWidth: 1,
    borderColor: COLORS.border,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    marginBottom: SPACING.md,
    color: COLORS.text,
  },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: SPACING.lg },
  centerText: { marginTop: SPACING.sm, color: COLORS.textMuted, textAlign: 'center' },
  stat: {
    flex: 1,
    minWidth: '45%',
    backgroundColor: COLORS.surface,
    borderRadius: 12,
    padding: SPACING.md,
    borderWidth: 1,
    borderColor: COLORS.border,
    marginBottom: SPACING.sm,
  },
  statValue: { fontSize: 22, fontWeight: '800', color: COLORS.primary },
  statLabel: { color: COLORS.textMuted, marginTop: 4 },
  listCard: {
    backgroundColor: COLORS.surface,
    borderRadius: 12,
    padding: SPACING.md,
    borderWidth: 1,
    borderColor: COLORS.border,
    marginBottom: SPACING.sm,
  },
  rowBetween: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  cardTitle: { fontSize: 16, color: COLORS.text, fontWeight: '700' as const },
  cardMeta: { color: COLORS.textMuted, marginTop: 4 },
  cardDesc: { color: COLORS.text, marginTop: 6 },
});
