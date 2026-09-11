import React, { useCallback, useState } from 'react';
import { Alert, RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useFocusEffect } from '@react-navigation/native';
import { Button, EmployeeCard, EmptyView, ErrorView, Header, Input, LoadingView, SearchBar } from '../../components/ui';
import { COLORS, SPACING } from '../../constants/config';
import { createEmployee, getDepartments, getEmployee, getEmployees, updateEmployee, updateEmployeeStatus } from '../../api/services';
import { getErrorMessage } from '../../api/client';
import type { Department, Employee } from '../../types';

export function EmployeeListScreen({ navigation }: NativeStackScreenProps<any>) {
  const [items, setItems] = useState<Employee[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (q = search) => {
    try {
      setError(null);
      const result = await getEmployees({ search: q || undefined, page: 1, pageSize: 50 });
      setItems(result.items);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [search]);

  useFocusEffect(useCallback(() => { setLoading(true); load(); }, [load]));

  if (loading) return <LoadingView message="Loading employees..." />;
  if (error) return <ErrorView message={error} onRetry={() => load()} />;

  return (
    <View style={styles.screen}>
      <ScrollView
        contentContainerStyle={{ padding: SPACING.md }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
      >
        <Header title="Employees" right={<Button title="Add" onPress={() => navigation.navigate('AddEmployee')} style={{ paddingHorizontal: 16, paddingVertical: 8 }} />} />
        <SearchBar value={search} onChangeText={(v) => { setSearch(v); }} placeholder="Search code, name, email" />
        <Button title="Search" onPress={() => load(search)} style={{ marginBottom: SPACING.md }} />
        {items.length === 0 ? <EmptyView message="No employees found." /> : items.map((e) => (
          <EmployeeCard key={e.id} employee={e} onPress={() => navigation.navigate('EmployeeDetails', { id: e.id })} />
        ))}
      </ScrollView>
    </View>
  );
}

export function AddEmployeeScreen({ navigation }: NativeStackScreenProps<any>) {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [form, setForm] = useState({
    employeeCode: '', firstName: '', lastName: '', email: '', phone: '', departmentId: '', designation: '', joiningDate: new Date().toISOString().slice(0, 10), password: 'Employee@123',
  });
  const [loading, setLoading] = useState(false);

  React.useEffect(() => {
    getDepartments().then(setDepartments).catch(() => undefined);
  }, []);

  const submit = async () => {
    try {
      setLoading(true);
      await createEmployee({
        ...form,
        departmentId: Number(form.departmentId || departments[0]?.id),
        joiningDate: form.joiningDate,
      });
      Alert.alert('Success', 'Employee created');
      navigation.goBack();
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <ScrollView contentContainerStyle={styles.form}>
      <Header title="Add Employee" />
      {Object.entries({
        employeeCode: 'Employee Code',
        firstName: 'First Name',
        lastName: 'Last Name',
        email: 'Email',
        phone: 'Phone',
        departmentId: 'Department ID',
        designation: 'Designation',
        joiningDate: 'Joining Date (YYYY-MM-DD)',
        password: 'Temporary Password',
      }).map(([key, label]) => (
        <Input
          key={key}
          label={label}
          value={(form as any)[key]}
          onChangeText={(v) => setForm((prev) => ({ ...prev, [key]: v }))}
          secureTextEntry={key === 'password'}
        />
      ))}
      <Button title="Create Employee" onPress={submit} loading={loading} />
    </ScrollView>
  );
}

export function EditEmployeeScreen({ route, navigation }: NativeStackScreenProps<any>) {
  const id = (route.params as { id: number }).id;
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', phone: '', departmentId: '', designation: '', joiningDate: '' });
  const [loading, setLoading] = useState(true);

  React.useEffect(() => {
    getEmployee(id).then((e) => {
      setForm({
        firstName: e.firstName,
        lastName: e.lastName,
        email: e.email,
        phone: e.phone ?? '',
        departmentId: String(e.departmentId),
        designation: e.designation,
        joiningDate: e.joiningDate.slice(0, 10),
      });
      setLoading(false);
    }).catch((e) => {
      Alert.alert('Error', getErrorMessage(e));
      setLoading(false);
    });
  }, [id]);

  if (loading) return <LoadingView />;

  const submit = async () => {
    try {
      await updateEmployee(id, {
        ...form,
        departmentId: Number(form.departmentId),
        joiningDate: form.joiningDate,
      });
      Alert.alert('Success', 'Employee updated');
      navigation.goBack();
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    }
  };

  return (
    <ScrollView contentContainerStyle={styles.form}>
      <Header title="Edit Employee" />
      {Object.entries({
        firstName: 'First Name', lastName: 'Last Name', email: 'Email', phone: 'Phone',
        departmentId: 'Department ID', designation: 'Designation', joiningDate: 'Joining Date',
      }).map(([key, label]) => (
        <Input key={key} label={label} value={(form as any)[key]} onChangeText={(v) => setForm((p) => ({ ...p, [key]: v }))} />
      ))}
      <Button title="Save Changes" onPress={submit} />
    </ScrollView>
  );
}

export function EmployeeDetailsScreen({ route, navigation }: NativeStackScreenProps<any>) {
  const id = (route.params as { id: number }).id;
  const [employee, setEmployee] = useState<Employee | null>(null);

  const load = useCallback(() => {
    getEmployee(id).then(setEmployee).catch((e) => Alert.alert('Error', getErrorMessage(e)));
  }, [id]);

  useFocusEffect(useCallback(() => { load(); }, [load]));

  if (!employee) return <LoadingView />;

  return (
    <ScrollView contentContainerStyle={styles.form}>
      <Header title={employee.employeeCode} subtitle={`${employee.firstName} ${employee.lastName}`} />
      <EmployeeCard employee={employee} />
      <Button title="Edit" onPress={() => navigation.navigate('EditEmployee', { id })} style={{ marginBottom: 8 }} />
      <Button
        title={employee.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}
        variant={employee.status === 'ACTIVE' ? 'danger' : 'secondary'}
        onPress={async () => {
          await updateEmployeeStatus(id, employee.status === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE');
          load();
        }}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: COLORS.background },
  form: { padding: SPACING.md, backgroundColor: COLORS.background },
});
