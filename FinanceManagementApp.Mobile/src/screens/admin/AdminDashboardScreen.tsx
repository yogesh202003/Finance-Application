import React, { useCallback, useState } from 'react';
import { Dimensions, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { LineChart, PieChart } from 'react-native-chart-kit';
import { Button, Card, EmptyView, ErrorView, ExpenseCard, Header, LoadingView, StatTile, TransactionCard } from '../../components/ui';
import { COLORS, SPACING } from '../../constants/config';
import { getAdminDashboard } from '../../api/services';
import { getErrorMessage } from '../../api/client';
import type { DashboardAdmin } from '../../types';
import { useAuth } from '../../hooks/useAuth';
import { useFocusEffect } from '@react-navigation/native';

export function AdminDashboardScreen({ navigation }: NativeStackScreenProps<any>) {
  const { session, logout } = useAuth();
  const [data, setData] = useState<DashboardAdmin | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setError(null);
      const dash = await getAdminDashboard();
      setData(dash);
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

  const width = Dimensions.get('window').width - 48;
  const chartConfig = {
    backgroundGradientFrom: COLORS.surface,
    backgroundGradientTo: COLORS.surface,
    color: (opacity = 1) => `rgba(11, 58, 83, ${opacity})`,
    labelColor: () => COLORS.textMuted,
    decimalPlaces: 0,
  };

  const pieData = data.categoryBreakdown.slice(0, 5).map((c, idx) => ({
    name: c.category,
    amount: c.amount,
    color: ['#0B3A53', '#1F8A70', '#B7791F', '#4C6A82', '#C0392B'][idx % 5],
    legendFontColor: COLORS.textMuted,
    legendFontSize: 11,
  }));

  return (
    <ScrollView
      style={styles.screen}
      contentContainerStyle={{ padding: SPACING.md }}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
    >
      <Header title="Admin Dashboard" subtitle={`Welcome, ${session?.name}`} right={<Button title="Logout" variant="ghost" onPress={logout} style={{ paddingHorizontal: 12, paddingVertical: 8 }} />} />
      <View style={styles.grid}>
        <StatTile label="Total Employees" value={data.totalEmployees} />
        <StatTile label="Active Employees" value={data.activeEmployees} />
        <StatTile label="Total Expenses" value={data.totalExpenses} />
        <StatTile label="Pending" value={data.pendingExpenses} />
        <StatTile label="Approved" value={data.approvedExpenses} />
        <StatTile label="Rejected" value={data.rejectedExpenses} />
        <StatTile label="Approved Amount" value={`$${data.totalExpenseAmount.toFixed(2)}`} />
      </View>

      <Card style={{ marginBottom: SPACING.md }}>
        <Text style={styles.section}>Monthly Expenses</Text>
        {data.monthlyExpenses.length > 0 ? (
          <LineChart
            data={{
              labels: data.monthlyExpenses.map((m) => m.month.split(' ')[0]),
              datasets: [{ data: data.monthlyExpenses.map((m) => m.amount || 0) }],
            }}
            width={width}
            height={200}
            chartConfig={chartConfig}
            bezier
            style={{ borderRadius: 12 }}
          />
        ) : (
          <Text style={styles.muted}>No monthly data yet.</Text>
        )}
      </Card>

      <Card style={{ marginBottom: SPACING.md }}>
        <Text style={styles.section}>By Category</Text>
        {pieData.length > 0 ? (
          <PieChart data={pieData} width={width} height={180} chartConfig={chartConfig} accessor="amount" backgroundColor="transparent" paddingLeft="8" />
        ) : (
          <Text style={styles.muted}>No category data yet.</Text>
        )}
      </Card>

      <Text style={styles.section}>Pending Approvals</Text>
      {data.pendingApprovals.length === 0 ? (
        <EmptyView message="No pending approvals." />
      ) : (
        data.pendingApprovals.map((e) => (
          <ExpenseCard key={e.id} expense={e} onPress={() => navigation.navigate('Approval', { id: e.id })} />
        ))
      )}

      <Text style={styles.section}>Recent Transactions</Text>
      {data.recentTransactions.map((t) => (
        <TransactionCard key={t.id} tx={t} />
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: COLORS.background },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: SPACING.md },
  section: { fontSize: 18, fontWeight: '700', color: COLORS.text, marginVertical: SPACING.sm },
  muted: { color: COLORS.textMuted },
});
