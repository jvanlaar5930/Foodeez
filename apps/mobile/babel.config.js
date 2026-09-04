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
          },
        },
      ],
      'react-native-reanimated/plugin',
    ],
  };
};
