import React, { useCallback, useState } from 'react';
import { Alert, RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useFocusEffect } from '@react-navigation/native';
import * as DocumentPicker from 'expo-document-picker';
import { Button, EmptyView, ErrorView, ExpenseCard, Header, Input, LoadingView, StatTile } from '../../components/ui';
import { COLORS, SPACING } from '../../constants/config';
import { createExpense, getCategories, getEmployeeDashboard, getMyExpenses } from '../../api/services';
import { getErrorMessage } from '../../api/client';
import type { DashboardEmployee, Expense, ExpenseCategory } from '../../types';
import { useAuth } from '../../hooks/useAuth';

export function EmployeeDashboardScreen({ navigation }: NativeStackScreenProps<any>) {
  const { session, logout } = useAuth();
  const [data, setData] = useState<DashboardEmployee | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setError(null);
      setData(await getEmployeeDashboard());
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { setLoading(true); load(); }, [load]));

  if (loading && !data) return <LoadingView message="Loading dashboard..." />;
  if (error && !data) return <ErrorView message={error} onRetry={load} />;
  if (!data) return <EmptyView message="No dashboard data." />;

  return (
    <ScrollView
      style={styles.screen}
      contentContainerStyle={{ padding: SPACING.md }}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
    >
      <Header title="My Dashboard" subtitle={`Welcome, ${session?.name}`} right={<Button title="Logout" variant="ghost" onPress={logout} style={{ paddingHorizontal: 12, paddingVertical: 8 }} />} />
      <View style={styles.grid}>
        <StatTile label="My Expenses" value={data.totalMyExpenses} />
        <StatTile label="Pending" value={data.pendingExpenses} />
        <StatTile label="Approved" value={data.approvedExpenses} />
        <StatTile label="Rejected" value={data.rejectedExpenses} />
        <StatTile label="Approved Amount" value={`$${data.approvedAmount.toFixed(2)}`} />
      </View>
      <Button title="Submit Expense" onPress={() => navigation.navigate('SubmitExpense')} style={{ marginBottom: SPACING.md }} />
      <Header title="Recent Expenses" />
      {data.recentExpenses.length === 0 ? (
        <EmptyView message="No expenses yet." />
      ) : (
        data.recentExpenses.map((e) => (
          <ExpenseCard key={e.id} expense={e} onPress={() => navigation.navigate('ExpenseDetails', { id: e.id })} />
        ))
      )}
    </ScrollView>
  );
}

export function SubmitExpenseScreen({ navigation }: NativeStackScreenProps<any>) {
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [categoryId, setCategoryId] = useState('');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [expenseDate, setExpenseDate] = useState(new Date().toISOString().slice(0, 10));
  const [receipt, setReceipt] = useState<DocumentPicker.DocumentPickerAsset | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  React.useEffect(() => {
    getCategories().then((cats) => {
      setCategories(cats.filter((c) => c.isActive));
      if (cats[0]) setCategoryId(String(cats[0].id));
    });
  }, []);

  const pickReceipt = async () => {
    const result = await DocumentPicker.getDocumentAsync({
      type: ['image/jpeg', 'image/png', 'application/pdf'],
      copyToCacheDirectory: true,
    });
    if (!result.canceled && result.assets?.[0]) {
      setReceipt(result.assets[0]);
    }
  };

  const validate = () => {
    const next: Record<string, string> = {};
    if (!categoryId) next.categoryId = 'Category is required';
    if (!amount || Number(amount) <= 0) next.amount = 'Amount must be greater than 0';
    if (!expenseDate) next.expenseDate = 'Expense date is required';
    if (!description || description.trim().length < 3) next.description = 'Description is required';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async () => {
    if (!validate()) return;
    try {
      setLoading(true);
      const form = new FormData();
      form.append('CategoryId', categoryId);
      form.append('Amount', amount);
      form.append('Description', description.trim());
      form.append('ExpenseDate', expenseDate);
      if (receipt) {
        form.append('receipt', {
          uri: receipt.uri,
          name: receipt.name ?? 'receipt.jpg',
          type: receipt.mimeType ?? 'image/jpeg',
        } as unknown as Blob);
      }
      await createExpense(form);
      Alert.alert('Success', 'Expense submitted and is now PENDING');
      navigation.navigate('MyExpenses');
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <ScrollView contentContainerStyle={{ padding: SPACING.md }} style={styles.screen}>
      <Header title="Submit Expense" />
      <Input label="Category ID" value={categoryId} onChangeText={setCategoryId} error={errors.categoryId} keyboardType="numeric" />
      {categories.length > 0 && (
        <View style={{ marginBottom: SPACING.md }}>
          {categories.map((c) => (
            <Button
              key={c.id}
              title={`${c.id}: ${c.name}${categoryId === String(c.id) ? ' ✓' : ''}`}
              variant={categoryId === String(c.id) ? 'secondary' : 'ghost'}
              onPress={() => setCategoryId(String(c.id))}
              style={{ marginBottom: 6, paddingVertical: 10 }}
            />
          ))}
        </View>
      )}
      <Input label="Amount" value={amount} onChangeText={setAmount} keyboardType="decimal-pad" error={errors.amount} />
      <Input label="Expense Date (YYYY-MM-DD)" value={expenseDate} onChangeText={setExpenseDate} error={errors.expenseDate} />
      <Input label="Description" value={description} onChangeText={setDescription} multiline error={errors.description} />
      <Button title={receipt ? `Receipt: ${receipt.name}` : 'Upload Receipt (JPG/PNG/PDF)'} variant="ghost" onPress={pickReceipt} style={{ marginBottom: SPACING.md }} />
      <Button title="Submit Expense" onPress={submit} loading={loading} />
    </ScrollView>
  );
}

export function MyExpensesScreen({ navigation }: NativeStackScreenProps<any>) {
  const [items, setItems] = useState<Expense[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setError(null);
      const result = await getMyExpenses({ page: 1, pageSize: 50 });
      setItems(result.items);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { setLoading(true); load(); }, [load]));

  if (loading) return <LoadingView message="Loading expenses..." />;
  if (error) return <ErrorView message={error} onRetry={load} />;

  return (
    <ScrollView
      style={styles.screen}
      contentContainerStyle={{ padding: SPACING.md }}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
    >
      <Header title="My Expenses" right={<Button title="New" onPress={() => navigation.navigate('SubmitExpense')} style={{ paddingHorizontal: 14, paddingVertical: 8 }} />} />
      {items.length === 0 ? <EmptyView message="No expenses found." /> : items.map((e) => (
        <ExpenseCard key={e.id} expense={e} onPress={() => navigation.navigate('ExpenseDetails', { id: e.id })} />
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: COLORS.background },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: SPACING.md },
});
