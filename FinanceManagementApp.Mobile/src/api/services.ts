import { api } from './client';
import type {
  ApiResponse,
  AuditLog,
  AuthSession,
  DashboardAdmin,
  DashboardEmployee,
  Department,
  Employee,
  Expense,
  ExpenseCategory,
  LoginResponse,
  NotificationItem,
  PagedResult,
  Profile,
  ReportDto,
  Transaction,
} from '../types';

export async function login(usernameOrEmail: string, password: string): Promise<AuthSession> {
  const { data } = await api.post<LoginResponse>('/api/auth/login', { usernameOrEmail, password });
  if (!data.success) throw new Error(data.message || 'Login failed');
  return {
    token: data.token,
    expiration: data.expiration,
    userId: data.userId,
    employeeId: data.employeeId,
    name: data.name,
    email: data.email,
    role: data.role,
  };
}

export async function changePassword(currentPassword: string, newPassword: string) {
  const { data } = await api.post<ApiResponse<object>>('/api/auth/change-password', {
    currentPassword,
    newPassword,
  });
  if (!data.success) throw new Error(data.message);
  return data;
}

export async function forgotPassword(email: string) {
  const { data } = await api.post<ApiResponse<{ resetToken?: string }>>('/api/auth/forgot-password', { email });
  return data;
}

export async function getAdminDashboard() {
  const { data } = await api.get<ApiResponse<DashboardAdmin>>('/api/dashboard/admin');
  return data.data!;
}

export async function getEmployeeDashboard() {
  const { data } = await api.get<ApiResponse<DashboardEmployee>>('/api/dashboard/employee');
  return data.data!;
}

export async function getEmployees(params: Record<string, string | number | undefined>) {
  const { data } = await api.get<ApiResponse<PagedResult<Employee>>>('/api/employees', { params });
  return data.data!;
}

export async function getEmployee(id: number) {
  const { data } = await api.get<ApiResponse<Employee>>(`/api/employees/${id}`);
  return data.data!;
}

export async function createEmployee(payload: Record<string, unknown>) {
  const { data } = await api.post<ApiResponse<Employee>>('/api/employees', payload);
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function updateEmployee(id: number, payload: Record<string, unknown>) {
  const { data } = await api.put<ApiResponse<Employee>>(`/api/employees/${id}`, payload);
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function updateEmployeeStatus(id: number, status: string) {
  const { data } = await api.patch<ApiResponse<object>>(`/api/employees/${id}/status`, { status });
  if (!data.success) throw new Error(data.message);
}

export async function getDepartments() {
  const { data } = await api.get<ApiResponse<Department[]>>('/api/departments');
  return data.data ?? [];
}

export async function createDepartment(name: string, description?: string) {
  const { data } = await api.post<ApiResponse<Department>>('/api/departments', { name, description });
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function getCategories() {
  const { data } = await api.get<ApiResponse<ExpenseCategory[]>>('/api/expense-categories');
  return data.data ?? [];
}

export async function getExpenses(params: Record<string, string | number | undefined>) {
  const { data } = await api.get<ApiResponse<PagedResult<Expense>>>('/api/expenses', { params });
  return data.data!;
}

export async function getMyExpenses(params: Record<string, string | number | undefined> = {}) {
  const { data } = await api.get<ApiResponse<PagedResult<Expense>>>('/api/expenses/my', { params });
  return data.data!;
}

export async function getPendingExpenses(page = 1) {
  const { data } = await api.get<ApiResponse<PagedResult<Expense>>>('/api/expenses/pending', {
    params: { page, pageSize: 20 },
  });
  return data.data!;
}

export async function getExpense(id: number) {
  const { data } = await api.get<ApiResponse<Expense>>(`/api/expenses/${id}`);
  return data.data!;
}

export async function createExpense(form: FormData) {
  const { data } = await api.post<ApiResponse<Expense>>('/api/expenses', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function approveExpense(id: number, comments?: string) {
  const { data } = await api.put<ApiResponse<Expense>>(`/api/expenses/${id}/approve`, { comments });
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function rejectExpense(id: number, comments: string) {
  const { data } = await api.put<ApiResponse<Expense>>(`/api/expenses/${id}/reject`, { comments });
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function getTransactions(page = 1) {
  const { data } = await api.get<ApiResponse<PagedResult<Transaction>>>('/api/transactions', {
    params: { page, pageSize: 20 },
  });
  return data.data!;
}

export async function getNotifications(page = 1) {
  const { data } = await api.get<ApiResponse<{
    unreadCount: number;
    items: NotificationItem[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  }>>('/api/notifications', { params: { page, pageSize: 20 } });
  return data.data!;
}

export async function markNotificationRead(id: number) {
  await api.put(`/api/notifications/${id}/read`);
}

export async function markAllNotificationsRead() {
  await api.put('/api/notifications/read-all');
}

export async function getAuditLogs(page = 1) {
  const { data } = await api.get<ApiResponse<PagedResult<AuditLog>>>('/api/auditlogs', {
    params: { page, pageSize: 20 },
  });
  return data.data!;
}

export async function getProfile() {
  const { data } = await api.get<ApiResponse<Profile>>('/api/profile');
  return data.data!;
}

export async function updateProfile(payload: { firstName: string; lastName: string; phone?: string }) {
  const { data } = await api.put<ApiResponse<Profile>>('/api/profile', payload);
  if (!data.success) throw new Error(data.message);
  return data.data!;
}

export async function getReport(type: 'monthly' | 'employee' | 'department' | 'category', params: Record<string, string | undefined> = {}) {
  const { data } = await api.get<ApiResponse<ReportDto>>(`/api/reports/${type}`, { params });
  return data.data!;
}
