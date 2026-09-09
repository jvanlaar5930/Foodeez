import { View } from 'react-native';

/**
 * Stands in for the brand SVGs in tests.
 *
 * Metro compiles an .svg import into a component, but jest runs the same import through babel
 * with no such transform, so the raw markup would arrive as a syntax error. No test asserts on
 * the artwork itself - only that a logo was rendered - so an empty View that still answers to
 * its testID is enough.
 */
export default function SvgMock({ testID }: { testID?: string }) {
  return <View testID={testID} />;
}
