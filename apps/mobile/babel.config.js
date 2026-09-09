module.exports = function (api) {
  api.cache(true);
  return {
    presets: ['babel-preset-expo'],
    plugins: [
      [
        'module-resolver',
        {
          root: ['./src'],
          alias: {
            '@': './src',
            // To source rather than dist, so there is no build step between editing shared
            // code and running the app - the same choice apps/web makes in vite.config.ts.
            '@foodeez/shared': '../../packages/shared/src',
            // Brand artwork shared with apps/web. Metro turns these SVGs into components -
            // see the transformer setup in metro.config.js.
            '@foodeez/brand': '../../packages/brand',
          },
        },
      ],
      'react-native-reanimated/plugin',
    ],
  };
};
