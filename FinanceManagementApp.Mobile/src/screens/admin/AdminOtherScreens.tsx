import React, { useCallback, useState } from 'react';
import { Alert, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useFocusEffect } from '@react-navigation/native';
import { Button, EmptyView, ErrorView, ExpenseCard, Header, Input, LoadingView, SearchBar, StatusBadge, TransactionCard, NotificationItemView } from '../../components/ui';
import { COLORS, SPACING } from '../../constants/config';
import {
  approveExpense, createDepartment, getAuditLogs, getDepartments, getExpense, getExpenses, getPendingExpenses,
  getNotifications, getReport, getTransactions, markAllNotificationsRead, markNotificationRead, rejectExpense,
} from '../../api/services';
import { getErrorMessage } from '../../api/client';
import type { AuditLog, Department, Expense, NotificationItem, ReportDto, Transaction } from '../../types';
import { useAuth } from '../../hooks/useAuth';

export function AdminExpenseListScreen({ navigation }: NativeStackScreenProps<any>) {
  const [items, setItems] = useState<Expense[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setError(null);
      const result = await getExpenses({ search: search || undefined, status: status || undefined, page: 1, pageSize: 50 });
      setItems(result.items);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [search, status]);

  useFocusEffect(useCallback(() => { setLoading(true); load(); }, [load]));

  if (loading) return <LoadingView message="Loading expenses..." />;
  if (error) return <ErrorView message={error} onRetry={load} />;

  return (
    <ScrollView style={styles.screen} contentContainerStyle={{ padding: SPACING.md }} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}>
      <Header title="All Expenses" />
      <SearchBar value={search} onChangeText={setSearch} placeholder="Search employee or description" />
      <Input label="Status filter (PENDING/APPROVED/REJECTED)" value={status} onChangeText={setStatus} />
      <Button title="Apply Filters" onPress={load} style={{ marginBottom: SPACING.md }} />
      {items.length === 0 ? <EmptyView message="No expenses found." /> : items.map((e) => (
        <ExpenseCard key={e.id} expense={e} onPress={() => navigation.navigate('ExpenseDetails', { id: e.id })} />
      ))}
    </ScrollView>
  );
}

export function PendingExpensesScreen({ navigation }: NativeStackScreenProps<any>) {
  const [items, setItems] = useState<Expense[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    try {
      const result = await getPendingExpenses();
      setItems(result.items);
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { setLoading(true); load(); }, [load]));
  if (loading) return <LoadingView message="Loading pending expenses..." />;

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="Pending Approvals" />
      {items.length === 0 ? <EmptyView message="No pending expenses." /> : items.map((e) => (
        <ExpenseCard key={e.id} expense={e} onPress={() => navigation.navigate('Approval', { id: e.id })} />
      ))}
    </ScrollView>
  );
}

export function ApprovalScreen({ route, navigation }: NativeStackScreenProps<any>) {
  const id = (route.params as { id: number }).id;
  const [expense, setExpense] = useState<Expense | null>(null);
  const [comments, setComments] = useState('');
  const [loading, setLoading] = useState(false);

  React.useEffect(() => {
    getExpense(id).then(setExpense).catch((e) => Alert.alert('Error', getErrorMessage(e)));
  }, [id]);

  if (!expense) return <LoadingView />;

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title={`Approve EXP${String(id).padStart(3, '0')}`} />
      <ExpenseCard expense={expense} />
      <Input label="Comments (required for rejection)" value={comments} onChangeText={setComments} multiline />
      <Button
        title="Approve"
        loading={loading}
        onPress={async () => {
          try {
            setLoading(true);
            await approveExpense(id, comments || undefined);
            Alert.alert('Approved', 'Expense approved and transaction created');
            navigation.goBack();
          } catch (e) {
            Alert.alert('Error', getErrorMessage(e));
          } finally {
            setLoading(false);
          }
        }}
        style={{ marginBottom: 8 }}
      />
      <Button
        title="Reject"
        variant="danger"
        loading={loading}
        onPress={async () => {
          try {
            setLoading(true);
            await rejectExpense(id, comments);
            Alert.alert('Rejected', 'Expense rejected');
            navigation.goBack();
          } catch (e) {
            Alert.alert('Error', getErrorMessage(e));
          } finally {
            setLoading(false);
          }
        }}
      />
    </ScrollView>
  );
}

export function ExpenseDetailsScreen({ route }: NativeStackScreenProps<any>) {
  const id = (route.params as { id: number }).id;
  const [expense, setExpense] = useState<Expense | null>(null);
  React.useEffect(() => {
    getExpense(id).then(setExpense).catch((e) => Alert.alert('Error', getErrorMessage(e)));
  }, [id]);
  if (!expense) return <LoadingView />;
  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title={`EXP${String(expense.id).padStart(3, '0')}`} />
      <ExpenseCard expense={expense} />
      <StatusBadge status={expense.status} />
      {!!expense.approvalComments && <Text style={{ marginTop: 12, color: COLORS.text }}>Comments: {expense.approvalComments}</Text>}
      {!!expense.receiptUrl && <Text style={{ marginTop: 8, color: COLORS.textMuted }}>Receipt: {expense.receiptUrl}</Text>}
    </ScrollView>
  );
}

export function DepartmentScreen() {
  const [items, setItems] = useState<Department[]>([]);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const load = useCallback(async () => {
    setItems(await getDepartments());
  }, []);

  useFocusEffect(useCallback(() => { load(); }, [load]));

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="Departments" />
      <Input label="Name" value={name} onChangeText={setName} />
      <Input label="Description" value={description} onChangeText={setDescription} />
      <Button title="Add Department" onPress={async () => {
        try {
          await createDepartment(name, description);
          setName(''); setDescription('');
          load();
        } catch (e) {
          Alert.alert('Error', getErrorMessage(e));
        }
      }} style={{ marginBottom: SPACING.md }} />
      {items.map((d) => (
        <View key={d.id} style={styles.rowCard}>
          <Text style={styles.title}>{d.name}</Text>
          <Text style={styles.muted}>{d.description}</Text>
        </View>
      ))}
    </ScrollView>
  );
}

export function TransactionScreen() {
  const [items, setItems] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const result = await getTransactions();
      setItems(result.items);
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { setLoading(true); load(); }, [load]));
  if (loading) return <LoadingView message="Loading transactions..." />;

  return (
    <ScrollView style={styles.screen} contentContainerStyle={{ padding: SPACING.md }} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}>
      <Header title="Transactions" />
      {items.length === 0 ? <EmptyView message="No transactions found." /> : items.map((t) => <TransactionCard key={t.id} tx={t} />)}
    </ScrollView>
  );
}

export function NotificationScreen() {
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [unread, setUnread] = useState(0);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    const result = await getNotifications();
    setItems(result.items);
    setUnread(result.unreadCount);
    setRefreshing(false);
  }, []);

  useFocusEffect(useCallback(() => { load().catch((e) => Alert.alert('Error', getErrorMessage(e))); }, [load]));

  return (
    <ScrollView style={styles.screen} contentContainerStyle={{ padding: SPACING.md }} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}>
      <Header title="Notifications" subtitle={`${unread} unread`} right={<Button title="Read all" variant="ghost" onPress={async () => { await markAllNotificationsRead(); load(); }} style={{ paddingHorizontal: 10, paddingVertical: 8 }} />} />
      {items.length === 0 ? <EmptyView message="No notifications." /> : items.map((n) => (
        <NotificationItemView key={n.id} item={n} onPress={async () => { await markNotificationRead(n.id); load(); }} />
      ))}
    </ScrollView>
  );
}

export function ReportsScreen() {
  const [report, setReport] = useState<ReportDto | null>(null);
  const [loading, setLoading] = useState(false);

  const run = async (type: 'monthly' | 'employee' | 'department' | 'category') => {
    try {
      setLoading(true);
      setReport(await getReport(type));
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="Reports" />
      <Button title="Monthly" onPress={() => run('monthly')} style={{ marginBottom: 8 }} />
      <Button title="By Employee" onPress={() => run('employee')} variant="secondary" style={{ marginBottom: 8 }} />
      <Button title="By Department" onPress={() => run('department')} variant="secondary" style={{ marginBottom: 8 }} />
      <Button title="By Category" onPress={() => run('category')} variant="secondary" style={{ marginBottom: 8 }} />
      {loading && <LoadingView message="Generating report..." />}
      {report && (
        <View style={styles.rowCard}>
          <Text style={styles.title}>{report.title}</Text>
          <Text style={styles.muted}>Total: ${report.totalAmount.toFixed(2)} · Count: {report.totalCount}</Text>
          {report.items.map((item) => (
            <Text key={item.label} style={{ marginTop: 6, color: COLORS.text }}>
              {item.label}: ${item.amount.toFixed(2)} ({item.count})
            </Text>
          ))}
        </View>
      )}
    </ScrollView>
  );
}

export function AuditLogScreen() {
  const [items, setItems] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);

  useFocusEffect(useCallback(() => {
    getAuditLogs().then((r) => setItems(r.items)).catch((e) => Alert.alert('Error', getErrorMessage(e))).finally(() => setLoading(false));
  }, []));

  if (loading) return <LoadingView message="Loading audit logs..." />;

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="Audit Logs" />
      {items.length === 0 ? <EmptyView message="No audit logs." /> : items.map((a) => (
        <View key={a.id} style={styles.rowCard}>
          <Text style={styles.title}>{a.action}</Text>
          <Text style={styles.muted}>{a.userEmail} · {a.entityName} #{a.entityId}</Text>
          <Text style={{ color: COLORS.text, marginTop: 4 }}>{a.description}</Text>
          <Text style={styles.muted}>{new Date(a.createdAt).toLocaleString()}</Text>
        </View>
      ))}
    </ScrollView>
  );
}

export function ProfileScreen({ navigation }: NativeStackScreenProps<any>) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('');

  React.useEffect(() => {
    import('../../api/services').then(({ getProfile }) =>
      getProfile().then((p) => {
        setFirstName(p.firstName ?? '');
        setLastName(p.lastName ?? '');
        setPhone(p.phone ?? '');
        setEmail(p.email);
        setRole(p.role);
      }),
    );
  }, []);

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="Profile" subtitle={`${email} · ${role}`} />
      <Input label="First Name" value={firstName} onChangeText={setFirstName} />
      <Input label="Last Name" value={lastName} onChangeText={setLastName} />
      <Input label="Phone" value={phone} onChangeText={setPhone} />
      <Button title="Save Profile" onPress={async () => {
        try {
          const { updateProfile } = await import('../../api/services');
          await updateProfile({ firstName, lastName, phone });
          Alert.alert('Saved', 'Profile updated');
        } catch (e) {
          Alert.alert('Error', getErrorMessage(e));
        }
      }} style={{ marginBottom: 8 }} />
      <Button title="Change Password" variant="ghost" onPress={() => navigation.navigate('ChangePassword')} />
    </ScrollView>
  );
}

export function SettingsScreen({ navigation }: NativeStackScreenProps<any>) {
  const { logout, role } = useAuth();
  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="More" />
      <Button title="Profile" onPress={() => navigation.navigate('Profile')} style={{ marginBottom: 8 }} />
      <Button title="Notifications" onPress={() => navigation.navigate('Notifications')} style={{ marginBottom: 8 }} />
      <Button title="Change Password" variant="secondary" onPress={() => navigation.navigate('ChangePassword')} style={{ marginBottom: 8 }} />
      {role === 'ADMIN' && (
        <>
          <Button title="All Expenses" onPress={() => navigation.navigate('ExpenseList')} style={{ marginBottom: 8 }} />
          <Button title="Departments" onPress={() => navigation.navigate('Departments')} style={{ marginBottom: 8 }} />
          <Button title="Transactions" onPress={() => navigation.navigate('Transactions')} style={{ marginBottom: 8 }} />
          <Button title="Audit Logs" onPress={() => navigation.navigate('AuditLogs')} style={{ marginBottom: 8 }} />
        </>
      )}
      <Button title="Logout" variant="danger" onPress={logout} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: COLORS.background },
  rowCard: { backgroundColor: COLORS.surface, borderRadius: 12, padding: SPACING.md, borderWidth: 1, borderColor: COLORS.border, marginBottom: SPACING.sm },
  title: { fontWeight: '700', color: COLORS.text, fontSize: 16 },
  muted: { color: COLORS.textMuted, marginTop: 4 },
});
