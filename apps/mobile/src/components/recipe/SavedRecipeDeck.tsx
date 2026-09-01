import React, { useRef } from 'react';
import {
  View,
  Text,
  Image,
  Animated,
  StyleSheet,
  Dimensions,
  TouchableOpacity,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { RecipeDto } from '@/types';

const SCREEN_WIDTH = Dimensions.get('window').width;
const CARD_WIDTH = Math.min(280, SCREEN_WIDTH * 0.72);
const CARD_HEIGHT = 300;
const GAP = Spacing.md;

/** Distance from one card's resting position to the next. */
const SNAP = CARD_WIDTH + GAP;

interface Props {
  recipes: RecipeDto[];
  onOpen: (recipe: RecipeDto) => void;
  onRemove: (recipe: RecipeDto) => void;
}

/**
 * The saved collection as a deck of cards laid out sideways.
 *
 * Cards are not paged. Paging - or `disableIntervalMomentum` - clamps every gesture to a
 * single card, which makes a long collection tedious to cross and kills the flick. Instead
 * the list keeps its momentum and only snaps once it has come to rest, so a light drag
 * moves one card and a hard flick sails through many.
 */
export function SavedRecipeDeck({ recipes, onOpen, onRemove }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  // A ref, not state: the scroll position drives native transforms and must never
  // re-render the list on the JS thread while a finger is down.
  const scrollX = useRef(new Animated.Value(0)).current;

  return (
    <Animated.FlatList
      horizontal
      data={recipes}
      keyExtractor={(r: RecipeDto) => r.id}
      showsHorizontalScrollIndicator={false}
      snapToInterval={SNAP}
      snapToAlignment="start"
      // "fast" would arrest the flick almost immediately; the default lets it carry.
      decelerationRate="normal"
      contentContainerStyle={styles.deckContent}
      scrollEventThrottle={16}
      onScroll={Animated.event([{ nativeEvent: { contentOffset: { x: scrollX } } }], {
        useNativeDriver: true,
      })}
      renderItem={({ item, index }: { item: RecipeDto; index: number }) => {
        // The card is centred when the list has scrolled exactly index * SNAP.
        const inputRange = [(index - 1) * SNAP, index * SNAP, (index + 1) * SNAP];

        const scale = scrollX.interpolate({
          inputRange,
          outputRange: [0.9, 1, 0.9],
          extrapolate: 'clamp',
        });
        // Neighbours sit lower and tilt away, so the stack reads as cards rather than tiles.
        const translateY = scrollX.interpolate({
          inputRange,
          outputRange: [18, 0, 18],
          extrapolate: 'clamp',
        });
        const rotate = scrollX.interpolate({
          inputRange,
          outputRange: ['5deg', '0deg', '-5deg'],
          extrapolate: 'clamp',
        });
        const opacity = scrollX.interpolate({
          inputRange,
          outputRange: [0.6, 1, 0.6],
          extrapolate: 'clamp',
        });
        // The photo drifts against the scroll, which reads as depth behind the card face.
        const imageShift = scrollX.interpolate({
          inputRange,
          outputRange: [24, 0, -24],
          extrapolate: 'clamp',
        });

        const totalTime = item.prepTimeMinutes + item.cookTimeMinutes;

        return (
          <Animated.View
            style={[styles.card, { opacity, transform: [{ scale }, { translateY }, { rotate }] }]}
          >
            <TouchableOpacity
              activeOpacity={0.9}
              style={styles.cardTouchable}
              onPress={() => onOpen(item)}
            >
              <View style={styles.cardImageWrap}>
                {item.imageUrl ? (
                  <Animated.Image
                    source={{ uri: item.imageUrl }}
                    style={[styles.cardImage, { transform: [{ translateX: imageShift }] }]}
                    resizeMode="cover"
                  />
                ) : (
                  <View style={styles.cardImagePlaceholder}>
                    <Ionicons name="restaurant" size={40} color={C.primary} />
                  </View>
                )}

                <TouchableOpacity
                  style={styles.removeButton}
                  hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                  onPress={() => onRemove(item)}
                  accessibilityLabel={`Remove ${item.name} from saved recipes`}
                >
                  <Ionicons name="bookmark" size={18} color={C.primary} />
                </TouchableOpacity>
              </View>

              <View style={styles.cardBody}>
                <Text style={styles.cardTitle} numberOfLines={2}>
                  {item.name}
                </Text>

                <View style={styles.cardMeta}>
                  <View style={styles.metaItem}>
                    <Ionicons name="time-outline" size={13} color={C.textSecondary} />
                    <Text style={styles.metaText}>{totalTime}m</Text>
                  </View>
                  <View style={styles.metaItem}>
                    <Ionicons name="flame-outline" size={13} color={C.textSecondary} />
                    <Text style={styles.metaText}>
                      {Math.round(item.nutritionalInfoPerServing.calories)} kcal
                    </Text>
                  </View>
                  <View style={styles.metaItem}>
                    <Ionicons name="people-outline" size={13} color={C.textSecondary} />
                    <Text style={styles.metaText}>{item.servings}</Text>
                  </View>
                </View>

                {item.description ? (
                  <Text style={styles.cardDesc} numberOfLines={3}>
                    {item.description}
                  </Text>
                ) : null}
              </View>
            </TouchableOpacity>
          </Animated.View>
        );
      }}
    />
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    deckContent: {
      paddingHorizontal: Spacing.md,
      gap: GAP,
      // Room for the tilt and drop of the neighbouring cards, which would otherwise clip.
      paddingVertical: Spacing.md,
    },
    card: {
      width: CARD_WIDTH,
      height: CARD_HEIGHT,
      borderRadius: BorderRadius.lg,
      backgroundColor: C.surface,
      overflow: 'hidden',
      ...Shadows.md,
    },
    cardTouchable: { flex: 1 },
    cardImageWrap: {
      height: 150,
      backgroundColor: C.primaryLight,
      overflow: 'hidden',
    },
    // Wider than the card so the parallax drift never exposes an edge.
    cardImage: {
      position: 'absolute',
      top: 0,
      bottom: 0,
      left: -24,
      right: -24,
    },
    cardImagePlaceholder: {
      ...StyleSheet.absoluteFillObject,
      alignItems: 'center',
      justifyContent: 'center',
    },
    removeButton: {
      position: 'absolute',
      top: Spacing.sm,
      right: Spacing.sm,
      width: 32,
      height: 32,
      borderRadius: 16,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: C.surface,
      ...Shadows.sm,
    },
    cardBody: { flex: 1, padding: Spacing.md, gap: Spacing.xs },
    cardTitle: { fontSize: FontSize.md, fontWeight: FontWeight.bold, color: C.text },
    cardMeta: { flexDirection: 'row', gap: Spacing.md, flexWrap: 'wrap' },
    metaItem: { flexDirection: 'row', alignItems: 'center', gap: 3 },
    metaText: { fontSize: FontSize.xs, color: C.textSecondary },
    cardDesc: { fontSize: FontSize.sm, color: C.textSecondary, lineHeight: 18 },
  });
