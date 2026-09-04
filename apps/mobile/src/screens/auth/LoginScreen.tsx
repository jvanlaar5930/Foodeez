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
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { describeApiError } from '@/utils/apiError';
import type { AuthStackParamList } from '@/navigation/types';

type LoginNav = NativeStackNavigationProp<AuthStackParamList, 'Login'>;

interface FormErrors {
  email?: string;
  password?: string;
}

function validateEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

export function LoginScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<LoginNav>();
  const login = useAuthStore((state) => state.login);
  const isLoading = useAuthStore((state) => state.isLoading);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});

  const validate = (): boolean => {
    const newErrors: FormErrors = {};
    if (!email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!validateEmail(email)) {
      newErrors.email = 'Please enter a valid email address';
    }
    if (!password) {
      newErrors.password = 'Password is required';
    } else if (password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleLogin = async () => {
    if (!validate()) return;
    try {
      await login({ email: email.trim(), password });
    } catch (err: unknown) {
      // Report what actually went wrong: a request that never reached the server is a
      // connectivity problem, not a rejected password.
      Alert.alert(
        'Login Failed',
        describeApiError(err, 'Invalid email or password. Please try again.'),
      );
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
          {/* Back button */}
          <TouchableOpacity style={styles.backBtn} onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color={C.text} />
          </TouchableOpacity>

          <View style={styles.titleSection}>
            <Text style={styles.title}>Welcome Back</Text>
            <Text style={styles.subtitle}>Sign in to continue tracking your nutrition</Text>
          </View>

          <View style={styles.form}>
            <Input
              label="Email Address"
              value={email}
              onChangeText={(text) => {
                setEmail(text);
                if (errors.email) setErrors((e) => ({ ...e, email: undefined }));
              }}
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
              value={password}
              onChangeText={(text) => {
                setPassword(text);
                if (errors.password) setErrors((e) => ({ ...e, password: undefined }));
              }}
              error={errors.password}
              secureTextEntry={!showPassword}
              autoCapitalize="none"
              autoComplete="password"
              placeholder="Enter your password"
              returnKeyType="done"
              onSubmitEditing={handleLogin}
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
                {showPassword ? 'Hide' : 'Show'} password
              </Text>
            </TouchableOpacity>

            <TouchableOpacity style={styles.forgotBtn}>
              <Text style={styles.forgotText}>Forgot Password?</Text>
            </TouchableOpacity>

            <Button
              title="Sign In"
              size="lg"
              fullWidth
              loading={isLoading}
              onPress={handleLogin}
            />
          </View>

          <TouchableOpacity
            style={styles.registerLink}
            onPress={() => navigation.navigate('Register')}
          >
            <Text style={styles.registerText}>
              Don't have an account?{' '}
              <Text style={styles.registerHighlight}>Register</Text>
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
  titleSection: {
    marginBottom: Spacing.xl,
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
    lineHeight: 22,
  },
  form: {
    gap: 0,
  },
  showPasswordBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    marginBottom: Spacing.sm,
    marginTop: -Spacing.sm,
  },
  showPasswordText: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
  },
  forgotBtn: {
    alignSelf: 'flex-end',
    marginBottom: Spacing.lg,
  },
  forgotText: {
    fontSize: FontSize.sm,
    color: C.primary,
    fontWeight: FontWeight.medium,
  },
  registerLink: {
    marginTop: Spacing.xl,
    alignItems: 'center',
  },
  registerText: {
    fontSize: FontSize.md,
    color: C.textSecondary,
  },
  registerHighlight: {
    color: C.primary,
    fontWeight: FontWeight.semibold,
  },
});
