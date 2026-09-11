import React, { useState } from 'react';
import { Alert, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { z } from 'zod';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Input } from '../../components/ui';
import { COLORS, SPACING } from '../../constants/config';
import { useAuth } from '../../hooks/useAuth';
import { forgotPassword } from '../../api/services';
import { getErrorMessage } from '../../api/client';

const loginSchema = z.object({
  usernameOrEmail: z.string().min(1, 'Username, email, or employee code is required'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type LoginForm = z.infer<typeof loginSchema>;

export function SplashScreen() {
  return (
    <View style={styles.splash}>
      <Text style={styles.brand}>FinanceApp</Text>
      <Text style={styles.splashSub}>Professional expense management</Text>
    </View>
  );
}

export function LoginScreen({ navigation }: NativeStackScreenProps<any>) {
  const { login } = useAuth();
  const [loading, setLoading] = useState(false);
  const { control, handleSubmit, formState: { errors } } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { usernameOrEmail: '', password: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      setLoading(true);
      await login(values.usernameOrEmail.trim(), values.password);
    } catch (e) {
      Alert.alert('Login failed', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  });

  return (
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={styles.brand}>FinanceApp</Text>
        <Text style={styles.subtitle}>Sign in to manage expenses</Text>
        <Controller
          control={control}
          name="usernameOrEmail"
          render={({ field: { value, onChange } }) => (
            <Input
              label="Username / Email / Employee Code"
              value={value}
              onChangeText={onChange}
              placeholder="admin@financeapp.com"
              error={errors.usernameOrEmail?.message}
              keyboardType="email-address"
            />
          )}
        />
        <Controller
          control={control}
          name="password"
          render={({ field: { value, onChange } }) => (
            <Input
              label="Password"
              value={value}
              onChangeText={onChange}
              placeholder="••••••••"
              secureTextEntry
              error={errors.password?.message}
            />
          )}
        />
        <Button title="Sign In" onPress={onSubmit} loading={loading} />
        <Button title="Forgot Password" onPress={() => navigation.navigate('ForgotPassword')} variant="ghost" style={{ marginTop: SPACING.sm }} />
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

export function ForgotPasswordScreen({ navigation }: NativeStackScreenProps<any>) {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);

  const submit = async () => {
    try {
      setLoading(true);
      const result = await forgotPassword(email.trim());
      const tokenHint = result.data?.resetToken ? `\n\nDev reset token:\n${result.data.resetToken}` : '';
      Alert.alert('Check your email', `${result.message}${tokenHint}`);
      navigation.goBack();
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.brand}>Reset Password</Text>
      <Input label="Email" value={email} onChangeText={setEmail} keyboardType="email-address" placeholder="you@company.com" />
      <Button title="Send Reset Link" onPress={submit} loading={loading} />
    </View>
  );
}

export function ChangePasswordScreen() {
  const [currentPassword, setCurrent] = useState('');
  const [newPassword, setNew] = useState('');
  const [loading, setLoading] = useState(false);

  const submit = async () => {
    try {
      setLoading(true);
      const { changePassword } = await import('../../api/services');
      await changePassword(currentPassword, newPassword);
      Alert.alert('Success', 'Password changed successfully');
      setCurrent('');
      setNew('');
    } catch (e) {
      Alert.alert('Error', getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.brand}>Change Password</Text>
      <Input label="Current Password" value={currentPassword} onChangeText={setCurrent} secureTextEntry />
      <Input label="New Password" value={newPassword} onChangeText={setNew} secureTextEntry />
      <Button title="Update Password" onPress={submit} loading={loading} />
    </View>
  );
}

const styles = StyleSheet.create({
  splash: { flex: 1, backgroundColor: COLORS.primary, alignItems: 'center', justifyContent: 'center' },
  brand: { fontSize: 32, fontWeight: '800', color: COLORS.primary, marginBottom: 8 },
  splashSub: { color: '#D7E8F2', marginTop: 8 },
  subtitle: { color: COLORS.textMuted, marginBottom: SPACING.lg },
  container: { flexGrow: 1, backgroundColor: COLORS.background, padding: SPACING.lg, justifyContent: 'center' },
});
