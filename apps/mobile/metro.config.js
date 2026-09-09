// Metro configuration for a package inside an npm workspace.
//
// Without this, `@foodeez/shared` resolves to a symlink pointing outside apps/mobile and Metro
// refuses it: by default it only watches the project directory and only looks in the project's
// own node_modules. The two settings below widen both, which is what lets the app import the
// shared package at all.
//
// Like the web app, this resolves the package to its TypeScript source rather than its built
// dist/, so there is no build step between editing shared code and seeing it in the app, and
// one resolution story across both clients.

const path = require('path');
const { getDefaultConfig } = require('expo/metro-config');

const projectRoot = __dirname;
const workspaceRoot = path.resolve(projectRoot, '../..');

const config = getDefaultConfig(projectRoot);

// Watch what the app can actually import: the shared package's source, and the hoisted
// node_modules the resolver falls back to.
//
// Watching the whole workspace was enough to make the bundle work, but it also put the .NET
// API's bin/obj, the web app's dist and .git into Metro's file crawl - thousands of files no
// bundle can reach, at a cost on Windows that shows up as a first bundle slow enough for
// Expo Go to give up on it ("failed to download remote update").
config.watchFolders = [
  path.resolve(workspaceRoot, 'packages/shared'),
  path.resolve(workspaceRoot, 'packages/brand'),
  path.resolve(workspaceRoot, 'node_modules'),
];

// Look in the app's node_modules first, then the hoisted root one.
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(workspaceRoot, 'node_modules'),
];

// npm hoists most dependencies to the root, so a package can legitimately be found in either
// place. Without this, a hoisted React and a local one can both be loaded, which breaks hooks.
config.resolver.disableHierarchicalLookup = true;

// Brand SVGs come out of @foodeez/brand as React components rather than image files.
//
// Metro treats .svg as an asset by default, which on React Native means a bitmap loader that
// cannot read vector markup - importing one yields an unrenderable object. Moving the
// extension from assetExts to sourceExts and handing it to react-native-svg-transformer
// compiles the markup into a react-native-svg component instead, so the same file the web app
// points an <img> at can be rendered directly here.
config.transformer.babelTransformerPath = require.resolve('react-native-svg-transformer');
config.resolver.assetExts = config.resolver.assetExts.filter((ext) => ext !== 'svg');
config.resolver.sourceExts = [...config.resolver.sourceExts, 'svg'];

module.exports = config;
