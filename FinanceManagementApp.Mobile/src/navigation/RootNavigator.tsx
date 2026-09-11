import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Text } from 'react-native';
import { useAuth } from '../hooks/useAuth';
import { SplashScreen, LoginScreen, ForgotPasswordScreen, ChangePasswordScreen } from '../screens/auth/AuthScreens';
import { AdminDashboardScreen } from '../screens/admin/AdminDashboardScreen';
import { AddEmployeeScreen, EditEmployeeScreen, EmployeeDetailsScreen, EmployeeListScreen } from '../screens/admin/EmployeeScreens';
import {
  AdminExpenseListScreen, ApprovalScreen, AuditLogScreen, DepartmentScreen, ExpenseDetailsScreen,
  NotificationScreen, PendingExpensesScreen, ProfileScreen, ReportsScreen, SettingsScreen, TransactionScreen,
} from '../screens/admin/AdminOtherScreens';
import { EmployeeDashboardScreen, MyExpensesScreen, SubmitExpenseScreen } from '../screens/employee/EmployeeScreens';
import { COLORS } from '../constants/config';
import { LoadingView } from '../components/ui';

const RootStack = createNativeStackNavigator();
const AuthStack = createNativeStackNavigator();
const AdminTabs = createBottomTabNavigator();
const EmployeeTabs = createBottomTabNavigator();
const AdminStack = createNativeStackNavigator();
const EmployeeStack = createNativeStackNavigator();

function TabLabel({ label, focused }: { label: string; focused: boolean }) {
  return <Text style={{ fontSize: 11, color: focused ? COLORS.primary : COLORS.textMuted, fontWeight: focused ? '700' : '500' }}>{label}</Text>;
}

function AuthNavigator() {
  return (
    <AuthStack.Navigator screenOptions={{ headerShown: false }}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
      <AuthStack.Screen name="ForgotPassword" component={ForgotPasswordScreen} options={{ headerShown: true, title: 'Forgot Password' }} />
    </AuthStack.Navigator>
  );
}

function AdminTabNavigator() {
  return (
    <AdminTabs.Navigator screenOptions={{ headerShown: false }}>
      <AdminTabs.Screen name="Dashboard" component={AdminDashboardScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Home" focused={focused} /> }} />
      <AdminTabs.Screen name="EmployeesTab" component={EmployeeListScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Employees" focused={focused} /> }} />
      <AdminTabs.Screen name="PendingTab" component={PendingExpensesScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Approvals" focused={focused} /> }} />
      <AdminTabs.Screen name="ReportsTab" component={ReportsScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Reports" focused={focused} /> }} />
      <AdminTabs.Screen name="MoreTab" component={SettingsScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="More" focused={focused} /> }} />
    </AdminTabs.Navigator>
  );
}

function EmployeeTabNavigator() {
  return (
    <EmployeeTabs.Navigator screenOptions={{ headerShown: false }}>
      <EmployeeTabs.Screen name="Dashboard" component={EmployeeDashboardScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Home" focused={focused} /> }} />
      <EmployeeTabs.Screen name="MyExpensesTab" component={MyExpensesScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Expenses" focused={focused} /> }} />
      <EmployeeTabs.Screen name="SubmitTab" component={SubmitExpenseScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Submit" focused={focused} /> }} />
      <EmployeeTabs.Screen name="TransactionsTab" component={TransactionScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="Txns" focused={focused} /> }} />
      <EmployeeTabs.Screen name="MoreTab" component={SettingsScreen} options={{ tabBarLabel: ({ focused }) => <TabLabel label="More" focused={focused} /> }} />
    </EmployeeTabs.Navigator>
  );
}

function AdminNavigator() {
  return (
    <AdminStack.Navigator>
      <AdminStack.Screen name="AdminTabs" component={AdminTabNavigator} options={{ headerShown: false }} />
      <AdminStack.Screen name="AddEmployee" component={AddEmployeeScreen} options={{ title: 'Add Employee' }} />
      <AdminStack.Screen name="EditEmployee" component={EditEmployeeScreen} options={{ title: 'Edit Employee' }} />
      <AdminStack.Screen name="EmployeeDetails" component={EmployeeDetailsScreen} options={{ title: 'Employee Details' }} />
      <AdminStack.Screen name="ExpenseList" component={AdminExpenseListScreen} options={{ title: 'Expenses' }} />
      <AdminStack.Screen name="ExpenseDetails" component={ExpenseDetailsScreen} options={{ title: 'Expense Details' }} />
      <AdminStack.Screen name="Approval" component={ApprovalScreen} options={{ title: 'Approval' }} />
      <AdminStack.Screen name="Departments" component={DepartmentScreen} options={{ title: 'Departments' }} />
      <AdminStack.Screen name="Transactions" component={TransactionScreen} options={{ title: 'Transactions' }} />
      <AdminStack.Screen name="Notifications" component={NotificationScreen} options={{ title: 'Notifications' }} />
      <AdminStack.Screen name="AuditLogs" component={AuditLogScreen} options={{ title: 'Audit Logs' }} />
      <AdminStack.Screen name="Profile" component={ProfileScreen} options={{ title: 'Profile' }} />
      <AdminStack.Screen name="ChangePassword" component={ChangePasswordScreen} options={{ title: 'Change Password' }} />
    </AdminStack.Navigator>
  );
}

function EmployeeNavigator() {
  return (
    <EmployeeStack.Navigator>
      <EmployeeStack.Screen name="EmployeeTabs" component={EmployeeTabNavigator} options={{ headerShown: false }} />
      <EmployeeStack.Screen name="SubmitExpense" component={SubmitExpenseScreen} options={{ title: 'Submit Expense' }} />
      <EmployeeStack.Screen name="MyExpenses" component={MyExpensesScreen} options={{ title: 'My Expenses' }} />
      <EmployeeStack.Screen name="ExpenseDetails" component={ExpenseDetailsScreen} options={{ title: 'Expense Details' }} />
      <EmployeeStack.Screen name="Notifications" component={NotificationScreen} options={{ title: 'Notifications' }} />
      <EmployeeStack.Screen name="Profile" component={ProfileScreen} options={{ title: 'Profile' }} />
      <EmployeeStack.Screen name="ChangePassword" component={ChangePasswordScreen} options={{ title: 'Change Password' }} />
    </EmployeeStack.Navigator>
  );
}

export function RootNavigator() {
  const { session, bootstrapping, role } = useAuth();

  if (bootstrapping) {
    return <LoadingView message="Starting FinanceApp..." />;
  }

  return (
    <NavigationContainer>
      <RootStack.Navigator screenOptions={{ headerShown: false }}>
        {!session ? (
          <RootStack.Screen name="Auth" component={AuthNavigator} />
        ) : role === 'ADMIN' ? (
          <RootStack.Screen name="Admin" component={AdminNavigator} />
        ) : (
          <RootStack.Screen name="Employee" component={EmployeeNavigator} />
        )}
      </RootStack.Navigator>
    </NavigationContainer>
  );
}
