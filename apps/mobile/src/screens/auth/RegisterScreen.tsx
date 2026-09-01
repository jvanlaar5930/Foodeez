import React, { useState } from 'react';
import {
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { useAuthStore } from '@/store/authStore';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { AuthStackParamList } from '@/navigation/types';

type RegisterNav = NativeStackNavigationProp<AuthStackParamList, 'Register'>;

interface FormErrors {
  firstName?: string;
  lastName?: string;
  email?: string;
  password?: string;
  confirmPassword?: string;
}

function validateEmail(email: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function getPasswordStrength(password: string, C: Palette): { level: number; label: string; color: string } {
  if (password.length === 0) return { level: 0, label: '', color: C.divider };
  let score = 0;
  if (password.length >= 8) score++;
  if (password.length >= 12) score++;
  if (/[A-Z]/.test(password)) score++;
  if (/[0-9]/.test(password)) score++;
  if (/[^A-Za-z0-9]/.test(password)) score++;

  if (score <= 1) return { level: 1, label: 'Weak', color: C.error };
  if (score <= 2) return { level: 2, label: 'Fair', color: C.warning };
  if (score <= 3) return { level: 3, label: 'Good', color: C.info };
  return { level: 4, label: 'Strong', color: C.primary };
}

export function RegisterScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<RegisterNav>();
  const { register, isLoading } = useAuthStore();

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});

  const strength = getPasswordStrength(form.password, C);

  const updateField = (field: keyof typeof form, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }));
  };

  const validate = (): boolean => {
    const newErrors: FormErrors = {};
    if (!form.firstName.trim()) newErrors.firstName = 'First name is required';
    if (!form.lastName.trim()) newErrors.lastName = 'Last name is required';
    if (!form.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!validateEmail(form.email)) {
      newErrors.email = 'Please enter a valid email address';
    }
    if (!form.password) {
      newErrors.password = 'Password is required';
    } else if (form.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters';
    }
    if (!form.confirmPassword) {
      newErrors.confirmPassword = 'Please confirm your password';
    } else if (form.password !== form.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleRegister = async () => {
    if (!validate()) return;
    try {
      await register({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        password: form.password,
      });
      navigation.navigate('ProfileSetup');
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string } }; message?: string; code?: string };
      const detail = axiosErr?.response?.data?.detail;
      const networkHint = axiosErr?.code === 'ERR_NETWORK' || axiosErr?.code === 'ECONNREFUSED'
        ? `\n\n(Network error: ${axiosErr.message})`
        : axiosErr?.message ? `\n\n(${axiosErr.message})` : '';
      Alert.alert('Registration Failed', (detail ?? 'Something went wrong. Please try again.') + networkHint);
    }
  };

  return (
    <SafeAreaView style={styles.safe}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <TouchableOpacity style={styles.backBtn} onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color={C.text} />
          </TouchableOpacity>

          <Text style={styles.title}>Create Account</Text>
          <Text style={styles.subtitle}>Start your nutrition journey today</Text>

          <View style={styles.nameRow}>
            <View style={styles.nameField}>
              <Input
                label="First Name"
                value={form.firstName}
                onChangeText={(v) => updateField('firstName', v)}
                error={errors.firstName}
                autoCapitalize="words"
                autoComplete="given-name"
                placeholder="John"
                returnKeyType="next"
              />
            </View>
            <View style={styles.nameField}>
              <Input
                label="Last Name"
                value={form.lastName}
                onChangeText={(v) => updateField('lastName', v)}
                error={errors.lastName}
                autoCapitalize="words"
                autoComplete="family-name"
                placeholder="Doe"
                returnKeyType="next"
              />
            </View>
          </View>

          <Input
            label="Email Address"
            value={form.email}
            onChangeText={(v) => updateField('email', v)}
            error={errors.email}
            keyboardType="email-address"
            autoCapitalize="none"
            autoComplete="email"
            autoCorrect={false}
            placeholder="you@example.com"
            returnKeyType="next"
          />

          <Input
            label="Password"
            value={form.password}
            onChangeText={(v) => updateField('password', v)}
            error={errors.password}
            secureTextEntry={!showPassword}
            autoCapitalize="none"
            placeholder="At least 8 characters"
            returnKeyType="next"
          />

          {/* Password strength indicator */}
          {form.password.length > 0 && (
            <View style={styles.strengthContainer}>
              <View style={styles.strengthBars}>
                {[1, 2, 3, 4].map((level) => (
                  <View
                    key={level}
                    style={[
                      styles.strengthBar,
                      {
                        backgroundColor:
                          strength.level >= level ? strength.color : C.divider,
                      },
                    ]}
                  />
                ))}
              </View>
              <Text style={[styles.strengthLabel, { color: strength.color }]}>
                {strength.label}
              </Text>
            </View>
          )}

          <Input
            label="Confirm Password"
            value={form.confirmPassword}
            onChangeText={(v) => updateField('confirmPassword', v)}
            error={errors.confirmPassword}
            secureTextEntry={!showPassword}
            autoCapitalize="none"
            placeholder="Re-enter your password"
            returnKeyType="done"
            onSubmitEditing={handleRegister}
          />

          <TouchableOpacity
            style={styles.showPasswordBtn}
            onPress={() => setShowPassword((prev) => !prev)}
          >
            <Ionicons
              name={showPassword ? 'eye-off-outline' : 'eye-outline'}
              size={20}
              color={C.textSecondary}
            />
            <Text style={styles.showPasswordText}>
              {showPassword ? 'Hide' : 'Show'} passwords
            </Text>
          </TouchableOpacity>

          <Button
            title="Create Account"
            size="lg"
            fullWidth
            loading={isLoading}
            onPress={handleRegister}
          />

          <TouchableOpacity
            style={styles.loginLink}
            onPress={() => navigation.navigate('Login')}
          >
            <Text style={styles.loginText}>
              Already have an account?{' '}
              <Text style={styles.loginHighlight}>Sign In</Text>
            </Text>
          </TouchableOpacity>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: C.background,
  },
  flex: {
    flex: 1,
  },
  content: {
    padding: Spacing.xl,
    flexGrow: 1,
  },
  backBtn: {
    width: 44,
    height: 44,
    justifyContent: 'center',
    marginBottom: Spacing.lg,
    marginLeft: -Spacing.sm,
  },
  title: {
    fontSize: FontSize.xxxl,
    fontWeight: FontWeight.bold,
    color: C.text,
    marginBottom: Spacing.xs,
  },
  subtitle: {
    fontSize: FontSize.md,
    color: C.textSecondary,
    marginBottom: Spacing.xl,
  },
  nameRow: {
    flexDirection: 'row',
    gap: Spacing.sm,
  },
  nameField: {
    flex: 1,
  },
  strengthContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: -Spacing.sm,
    marginBottom: Spacing.md,
    gap: Spacing.sm,
  },
  strengthBars: {
    flex: 1,
    flexDirection: 'row',
    gap: 4,
  },
  strengthBar: {
    flex: 1,
    height: 4,
    borderRadius: BorderRadius.full,
  },
  strengthLabel: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    minWidth: 45,
  },
  showPasswordBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    marginBottom: Spacing.lg,
    marginTop: -Spacing.sm,
  },
  showPasswordText: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
  },
  loginLink: {
    marginTop: Spacing.xl,
    alignItems: 'center',
    paddingBottom: Spacing.md,
  },
  loginText: {
    fontSize: FontSize.md,
    color: C.textSecondary,
  },
  loginHighlight: {
    color: C.primary,
    fontWeight: FontWeight.semibold,
  },
});
