/**
 * Types the imports of shared brand artwork.
 *
 * Metro hands .svg files to react-native-svg-transformer (see metro.config.js), so an import
 * yields a react-native-svg component rather than the URL string the web app gets from Vite.
 * The transformer ships no declarations of its own, so this states the shape.
 *
 * Bitmaps stay ordinary Metro assets: the import is an opaque handle for <Image source>, not
 * a path, which is why it is typed as a number rather than a string.
 */
declare module '*.svg' {
  import type React from 'react';
  import type { SvgProps } from 'react-native-svg';

  const content: React.FC<SvgProps>;
  export default content;
}

declare module '*.png' {
  const content: number;
  export default content;
}
