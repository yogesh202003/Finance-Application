export type Role = 'ADMIN' | 'EMPLOYEE';

export interface LoginResponse {
  success: boolean;
  message: string;
  token: string;
  expiration: string;
  userId: number;
  employeeId?: number | null;
  name: string;
  email: string;
  role: Role;
}

export interface AuthSession {
  token: string;
  expiration: string;
  userId: number;
  employeeId?: number | null;
  name: string;
  email: string;
  role: Role;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Employee {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  email: string;
  phone?: string;
  departmentId: number;
  departmentName: string;
  designation: string;
  joiningDate: string;
  status: string;
}

export interface Department {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface ExpenseCategory {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface Expense {
  id: number;
  expenseCode?: string;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  categoryId: number;
  categoryName: string;
  amount: number;
  description: string;
  expenseDate: string;
  receiptUrl?: string;
  status: 'PENDING' | 'APPROVED' | 'REJECTED' | string;
  createdAt: string;
  approvalComments?: string;
}

export interface Transaction {
  id: number;
  employeeId: number;
  employeeName: string;
  expenseId: number;
  transactionType: string;
  amount: number;
  description: string;
  transactionDate: string;
  referenceNumber: string;
}

export interface NotificationItem {
  id: number;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface DashboardAdmin {
  totalEmployees: number;
  activeEmployees: number;
  totalExpenses: number;
  pendingExpenses: number;
  approvedExpenses: number;
  rejectedExpenses: number;
  totalExpenseAmount: number;
  monthlyExpenses: { month: string; amount: number; count: number }[];
  categoryBreakdown: { category: string; amount: number; count: number }[];
  recentTransactions: Transaction[];
  pendingApprovals: Expense[];
}

export interface DashboardEmployee {
  totalMyExpenses: number;
  pendingExpenses: number;
  approvedExpenses: number;
  rejectedExpenses: number;
  approvedAmount: number;
  recentExpenses: Expense[];
}

export interface AuditLog {
  id: number;
  userId?: number;
  userEmail?: string;
  action: string;
  entityName: string;
  entityId?: string;
  description?: string;
  createdAt: string;
}

export interface Profile {
  userId: number;
  username: string;
  email: string;
  role: Role;
  employeeId?: number;
  firstName?: string;
  lastName?: string;
  phone?: string;
  departmentName?: string;
  designation?: string;
}

export interface ReportDto {
  title: string;
  totalAmount: number;
  totalCount: number;
  items: { label: string; amount: number; count: number; status?: string }[];
}
