import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { Button } from '@/components/ui/Button';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { AuthStackParamList } from '@/navigation/types';
import { Logo } from '@/components/ui/Logo';

type WelcomeNav = NativeStackNavigationProp<AuthStackParamList, 'Welcome'>;

const FEATURES = [
  {
    icon: 'nutrition-outline' as const,
    title: 'Track Every Meal',
    description: 'Log meals instantly with barcode scanning or AI photo recognition',
  },
  {
    icon: 'calendar-outline' as const,
    title: 'Plan Your Week',
    description: 'Get personalized AI meal plans tailored to your goals',
  },
  {
    icon: 'analytics-outline' as const,
    title: 'Smart AI Insights',
    description: 'Receive actionable recommendations to hit your nutrition targets',
  },
];

export function WelcomeScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<WelcomeNav>();

  return (
    <SafeAreaView style={styles.safe}>
      <View style={styles.container}>
        {/* Logo Section */}
        <View style={styles.logoSection}>
          <Logo height={52} />
          <Text style={styles.tagline}>Your AI-powered nutrition companion</Text>
        </View>

        {/* Features */}
        <View style={styles.features}>
          {FEATURES.map((feature) => (
            <View key={feature.title} style={styles.featureRow}>
              <View style={styles.featureIcon}>
                <Ionicons name={feature.icon} size={24} color={C.primary} />
              </View>
              <View style={styles.featureText}>
                <Text style={styles.featureTitle}>{feature.title}</Text>
                <Text style={styles.featureDesc}>{feature.description}</Text>
              </View>
            </View>
          ))}
        </View>

        {/* CTA Buttons */}
        <View style={styles.buttons}>
          <Button
            title="Get Started"
            size="lg"
            fullWidth
            onPress={() => navigation.navigate('Register')}
          />
          <TouchableOpacity
            style={styles.loginLink}
            onPress={() => navigation.navigate('Login')}
          >
            <Text style={styles.loginLinkText}>I already have an account</Text>
          </TouchableOpacity>
        </View>
      </View>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: C.background,
  },
  container: {
    flex: 1,
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.lg,
    justifyContent: 'space-between',
  },
  logoSection: {
    alignItems: 'center',
    paddingTop: Spacing.xxl,
  },
  tagline: {
    fontSize: FontSize.lg,
    color: C.textSecondary,
    marginTop: Spacing.md,
    textAlign: 'center',
  },
  features: {
    gap: Spacing.lg,
    paddingVertical: Spacing.xl,
  },
  featureRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.md,
  },
  featureIcon: {
    width: 52,
    height: 52,
    borderRadius: 14,
    backgroundColor: C.primaryLight,
    justifyContent: 'center',
    alignItems: 'center',
    flexShrink: 0,
  },
  featureText: {
    flex: 1,
  },
  featureTitle: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: C.text,
    marginBottom: 2,
  },
  featureDesc: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    lineHeight: 20,
  },
  buttons: {
    gap: Spacing.md,
    paddingBottom: Spacing.md,
  },
  loginLink: {
    alignItems: 'center',
    paddingVertical: Spacing.sm,
  },
  loginLinkText: {
    fontSize: FontSize.md,
    color: C.primary,
    fontWeight: FontWeight.medium,
  },
});
