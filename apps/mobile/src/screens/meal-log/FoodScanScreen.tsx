import React, { useState, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  FlatList,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { CameraView, CameraType, useCameraPermissions } from 'expo-camera';
import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { MealLogStackParamList } from '@/navigation/types';
import { mealService } from '@/services/mealService';
import { describeApiError } from '@/utils/apiError';
import { QuickAddItemDto } from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<MealLogStackParamList, 'FoodScan'>;

type ScanState = 'camera' | 'analyzing' | 'results';

export function FoodScanScreen({ navigation }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [permission, requestPermission] = useCameraPermissions();
  const [facing, setFacing] = useState<CameraType>('back');
  const [scanState, setScanState] = useState<ScanState>('camera');
  const [parsedItems, setParsedItems] = useState<QuickAddItemDto[]>([]);
  const [selectedItems, setSelectedItems] = useState<Set<number>>(new Set());
  const cameraRef = useRef<CameraView>(null);

  if (!permission) return <View />;

  if (!permission.granted) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.permissionContainer}>
          <Ionicons name="camera-outline" size={64} color={C.textSecondary} />
          <Text style={styles.permissionTitle}>Camera Access Required</Text>
          <Text style={styles.permissionText}>
            Foodeez needs camera access to scan and analyze your food.
          </Text>
          <TouchableOpacity style={styles.permissionButton} onPress={requestPermission}>
            <Text style={styles.permissionButtonText}>Grant Permission</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const handleCapture = async () => {
    if (!cameraRef.current) return;
    try {
      const photo = await cameraRef.current.takePictureAsync({ base64: false, quality: 0.8 });
      if (!photo?.uri) return;
      setScanState('analyzing');
      const result = await mealService.parseFoodImage(photo.uri);

      // No items is not an empty meal, it is a photo that could not be read. Saying so and
      // going back to the camera beats an empty results list with a confirm button on it.
      if (result.items.length === 0) {
        Alert.alert(
          'Nothing recognised',
          result.note ?? 'No food could be picked out of that photo. Try a clearer shot.',
        );
        setScanState('camera');
        return;
      }

      setParsedItems(result.items);
      setSelectedItems(new Set(result.items.map((_, i) => i)));
      setScanState('results');
    } catch (err: unknown) {
      Alert.alert(
        'Analysis Failed',
        describeApiError(err, 'Could not analyze the food. Please try again or add manually.'),
      );
      setScanState('camera');
    }
  };

  const toggleItem = (index: number) => {
    setSelectedItems(prev => {
      const next = new Set(prev);
      if (next.has(index)) next.delete(index);
      else next.add(index);
      return next;
    });
  };

  const handleConfirm = () => {
    const confirmed = parsedItems.filter((_, i) => selectedItems.has(i));
    navigation.navigate('AddMeal', { parsedItems: confirmed } as any);
  };

  if (scanState === 'analyzing') {
    return (
      <SafeAreaView style={[styles.container, styles.centered]}>
        <LoadingSpinner />
        <Text style={styles.analyzingText}>Analyzing your food...</Text>
        <Text style={styles.analyzingSubText}>AI is identifying ingredients and nutrition</Text>
      </SafeAreaView>
    );
  }

  if (scanState === 'results') {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.resultsHeader}>
          <Text style={styles.resultsTitle}>Food Detected</Text>
          <Text style={styles.resultsSubtitle}>Select items to add to your meal log</Text>
        </View>
        <FlatList
          data={parsedItems}
          keyExtractor={(_, i) => i.toString()}
          contentContainerStyle={styles.resultsList}
          renderItem={({ item, index }) => (
            <TouchableOpacity
              style={[styles.resultItem, selectedItems.has(index) && styles.resultItemSelected]}
              onPress={() => toggleItem(index)}
            >
              <View style={styles.resultCheckbox}>
                {selectedItems.has(index) && (
                  <Ionicons name="checkmark" size={16} color={C.onPrimary} />
                )}
              </View>
              <View style={styles.resultContent}>
                <Text style={styles.resultName}>{item.foodItem.name}</Text>
                {item.foodItem.brand && <Text style={styles.resultBrand}>{item.foodItem.brand}</Text>}
                <Text style={styles.resultServing}>
                  {item.quantity} {item.unit}
                </Text>
              </View>
              <View style={styles.resultNutrition}>
                <Text style={styles.resultCalories}>
                  {Math.round(
                    item.foodItem.nutritionalInfo.calories *
                      (item.foodItem.servingSize > 0 ? item.quantity / item.foodItem.servingSize : item.quantity),
                  )}{' '}
                  kcal
                </Text>
                {/* Which numbers were looked up and which were guessed at, so an estimate can
                    be checked before it is logged rather than taken on faith. */}
                <Text style={styles.resultConfidence}>
                  {item.source === 'Estimated' ? 'AI estimate' : 'from database'}
                </Text>
              </View>
            </TouchableOpacity>
          )}
        />
        <View style={styles.resultsActions}>
          <TouchableOpacity style={styles.retryButton} onPress={() => setScanState('camera')}>
            <Ionicons name="camera-outline" size={20} color={C.primary} />
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.confirmButton, selectedItems.size === 0 && styles.confirmButtonDisabled]}
            onPress={handleConfirm}
            disabled={selectedItems.size === 0}
          >
            <Text style={styles.confirmButtonText}>
              Add {selectedItems.size} Item{selectedItems.size !== 1 ? 's' : ''}
            </Text>
            <Ionicons name="arrow-forward" size={20} color={C.onPrimary} />
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <View style={styles.container}>
      <CameraView ref={cameraRef} style={styles.camera} facing={facing}>
        <SafeAreaView style={styles.cameraOverlay}>
          <TouchableOpacity style={styles.closeButton} onPress={() => navigation.goBack()}>
            <Ionicons name="close" size={28} color="#FFFFFF" />
          </TouchableOpacity>
          <View style={styles.scanFrame} />
          <Text style={styles.scanHint}>Center your food in the frame</Text>
          <View style={styles.cameraControls}>
            <TouchableOpacity
              style={styles.flipButton}
              onPress={() => setFacing(f => (f === 'back' ? 'front' : 'back'))}
            >
              <Ionicons name="camera-reverse-outline" size={28} color="#FFFFFF" />
            </TouchableOpacity>
            <TouchableOpacity style={styles.captureButton} onPress={handleCapture}>
              <View style={styles.captureButtonInner} />
            </TouchableOpacity>
            <View style={{ width: 52 }} />
          </View>
        </SafeAreaView>
      </CameraView>
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  centered: { justifyContent: 'center', alignItems: 'center', gap: Spacing.md },
  camera: { flex: 1 },
  cameraOverlay: {
    flex: 1,
    backgroundColor: 'transparent',
    justifyContent: 'space-between',
    padding: Spacing.lg,
  },
  closeButton: {
    alignSelf: 'flex-start',
    backgroundColor: 'rgba(0,0,0,0.4)',
    borderRadius: BorderRadius.full,
    padding: Spacing.sm,
  },
  scanFrame: {
    width: 280,
    height: 280,
    alignSelf: 'center',
    borderWidth: 2,
    // Drawn over the camera preview, so white regardless of theme.
    borderColor: '#FFFFFF',
    borderRadius: BorderRadius.lg,
    backgroundColor: 'transparent',
  },
  scanHint: {
    textAlign: 'center',
    // On a black scrim over the live camera, so it is white in both themes - the theme's
    // on-accent ink would be unreadable there.
    color: '#FFFFFF',
    fontSize: FontSize.md,
    backgroundColor: 'rgba(0,0,0,0.4)',
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
    borderRadius: BorderRadius.md,
    alignSelf: 'center',
  },
  cameraControls: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingBottom: Spacing.lg,
  },
  flipButton: {
    backgroundColor: 'rgba(0,0,0,0.4)',
    borderRadius: BorderRadius.full,
    padding: Spacing.sm,
    width: 52,
    height: 52,
    alignItems: 'center',
    justifyContent: 'center',
  },
  captureButton: {
    width: 72,
    height: 72,
    borderRadius: 36,
    backgroundColor: 'rgba(255,255,255,0.3)',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 3,
    borderColor: C.surface,
  },
  captureButtonInner: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: C.surface,
  },
  permissionContainer: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: Spacing.xl },
  permissionTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, marginTop: Spacing.md, color: C.text },
  permissionText: { fontSize: FontSize.md, color: C.textSecondary, textAlign: 'center', marginTop: Spacing.sm },
  permissionButton: {
    marginTop: Spacing.xl,
    backgroundColor: C.primary,
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md,
    borderRadius: BorderRadius.lg,
  },
  permissionButtonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  analyzingText: { fontSize: FontSize.xl, fontWeight: FontWeight.semibold, color: C.text },
  analyzingSubText: { fontSize: FontSize.md, color: C.textSecondary },
  resultsHeader: { padding: Spacing.lg, borderBottomWidth: 1, borderBottomColor: C.divider },
  resultsTitle: { fontSize: FontSize.xxl, fontWeight: FontWeight.bold, color: C.text },
  resultsSubtitle: { fontSize: FontSize.md, color: C.textSecondary, marginTop: Spacing.xs },
  resultsList: { padding: Spacing.md, gap: Spacing.sm },
  resultItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    borderWidth: 2,
    borderColor: 'transparent',
    ...Shadows.sm,
  },
  resultItemSelected: { borderColor: C.primary },
  resultCheckbox: {
    width: 24,
    height: 24,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: C.primary,
    backgroundColor: C.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: Spacing.md,
  },
  resultContent: { flex: 1 },
  resultName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  resultBrand: { fontSize: FontSize.sm, color: C.textSecondary },
  resultServing: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
  resultNutrition: { alignItems: 'flex-end' },
  resultCalories: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  resultConfidence: { fontSize: FontSize.xs, color: C.textSecondary },
  resultsActions: {
    flexDirection: 'row',
    gap: Spacing.md,
    padding: Spacing.lg,
    borderTopWidth: 1,
    borderTopColor: C.divider,
  },
  retryButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 2,
    borderColor: C.primary,
    borderRadius: BorderRadius.lg,
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.md,
  },
  retryButtonText: { color: C.primary, fontWeight: FontWeight.semibold },
  confirmButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.xs,
    backgroundColor: C.primary,
    borderRadius: BorderRadius.lg,
    paddingVertical: Spacing.md,
  },
  confirmButtonDisabled: { backgroundColor: C.textHint },
  confirmButtonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
});
