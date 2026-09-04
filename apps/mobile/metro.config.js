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

// Watch the whole workspace, so edits in packages/shared trigger a rebuild.
config.watchFolders = [workspaceRoot];

// Look in the app's node_modules first, then the hoisted root one.
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(workspaceRoot, 'node_modules'),
];

// npm hoists most dependencies to the root, so a package can legitimately be found in either
// place. Without this, a hoisted React and a local one can both be loaded, which breaks hooks.
config.resolver.disableHierarchicalLookup = true;

module.exports = config;
