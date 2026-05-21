import { useState, useCallback } from 'react';
import { Alert, Platform } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { useCameraPermissions } from 'expo-camera';

export function useCamera() {
  const [cameraPermission, requestCameraPermission] = useCameraPermissions();
  const [isCapturing, setIsCapturing] = useState(false);

  const hasPermission = cameraPermission?.granted ?? false;

  const requestPermission = useCallback(async (): Promise<boolean> => {
    const result = await requestCameraPermission();
    return result.granted;
  }, [requestCameraPermission]);

  const pickFromGallery = useCallback(async (): Promise<string | null> => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert(
        'Permission Required',
        'Please allow access to your photo library to select food photos.',
      );
      return null;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      allowsEditing: true,
      aspect: [4, 3],
      quality: 0.8,
    });

    if (result.canceled || result.assets.length === 0) {
      return null;
    }

    return result.assets[0].uri;
  }, []);

  const captureImage = useCallback(async (): Promise<string | null> => {
    if (!hasPermission) {
      const granted = await requestPermission();
      if (!granted) {
        Alert.alert(
          'Permission Required',
          'Please allow camera access to scan food items.',
        );
        return null;
      }
    }

    setIsCapturing(true);
    try {
      const result = await ImagePicker.launchCameraAsync({
        allowsEditing: true,
        aspect: [4, 3],
        quality: 0.8,
      });

      if (result.canceled || result.assets.length === 0) {
        return null;
      }

      return result.assets[0].uri;
    } finally {
      setIsCapturing(false);
    }
  }, [hasPermission, requestPermission]);

  return {
    hasPermission,
    requestPermission,
    captureImage,
    pickFromGallery,
    isCapturing,
    // Android-specific: check if we're on a physical device
    isAvailable: Platform.OS !== 'web',
  };
}
