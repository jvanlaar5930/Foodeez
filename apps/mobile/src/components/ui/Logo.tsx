import { Image, View } from 'react-native';
import Wordmark from '@foodeez/brand/assets/Foodeez_image.svg';
import mark from '@foodeez/brand/assets/Foodeez_mark.png';

/**
 * The horizontal Foodeez lockup: icon mark beside the wordmark.
 *
 * Composed from the two assets rather than shipped as one image, so the wordmark stays true
 * vector at any size - Metro turns the SVG into a react-native-svg component, while the mark
 * is still a bitmap and comes through as an <Image>.
 *
 * Both files are trimmed to their artwork, so a single height drives the layout and the
 * widths follow from these ratios. The wordmark is set a little shorter than the mark: it
 * carries its own tagline under the name, so matching heights exactly leaves a wide block of
 * text overpowering the icon.
 */
const MARK_RATIO = 1084 / 959;
const WORDMARK_RATIO = 1644 / 397;
const WORDMARK_SCALE = 0.8;

type LogoProps = {
  /** Height of the icon mark in points; everything else is derived from it. */
  height?: number;
};

export function Logo({ height = 32 }: LogoProps) {
  const wordmarkHeight = Math.round(height * WORDMARK_SCALE);

  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: Math.round(height * 0.28) }}>
      <Image
        source={mark}
        style={{ width: Math.round(height * MARK_RATIO), height }}
        resizeMode="contain"
        accessibilityIgnoresInvertColors
      />
      <Wordmark
        width={Math.round(wordmarkHeight * WORDMARK_RATIO)}
        height={wordmarkHeight}
        accessibilityLabel="Foodeez"
      />
    </View>
  );
}
