# @foodeez/brand

One home for the brand artwork, so a logo change lands in both clients at once instead of
being copied into `apps/web/src/assets` and `apps/mobile/assets` and drifting apart.

Nothing here is built or published. Both apps resolve the package straight to these files
through a bundler alias, the same way they resolve `@foodeez/shared` to its TypeScript
source rather than a `dist/`.

## Layout

- `assets/` - artwork imported from code. Referenced as `@foodeez/brand/assets/<name>`.
- `public/` - files the web app serves verbatim at its root, which today means the favicon.
  `apps/web/index.html` asks for `/favicon.svg`, and Vite's `publicDir` points here.

The split matters: Vite refuses to let JavaScript import a file that lives in `publicDir`,
so a given file belongs in one folder or the other, not both.

## What is here

| File | Use |
| --- | --- |
| `Foodeez_image.svg` | Wordmark + tagline + leaf. True vector, ~18KB. |
| `Foodeez_mark.png` | Icon mark, transparent, 480x425. |
| `Foodeez_logo.svg` | Source of the mark. **Do not import** - see below. |
| `Foodeez_full_logo.svg` | The original combined lockup. Unused - see below. |

There is deliberately no combined lockup asset. The apps compose one from the mark and the
wordmark, in `AppLogo.vue` on web and `Logo.tsx` on mobile, which keeps the wordmark true
vector at any size and means one set of files serves every placement.

Both assets are trimmed to their artwork, so the components size everything from a single
height. The ratios they rely on are the intrinsic ones: 1084x959 for the mark, 1644x397 for
the wordmark. If either file is replaced with differently-cropped artwork, update those.

### The two source files

`Foodeez_logo.svg` is a *pixel trace*, not a drawing: 39,414 `<path>` elements covering
410,770 one-pixel rectangles in 23,748 colours, at 8.7MB. On web that is seconds of parse and
layout; on mobile `react-native-svg` would try to build 39,414 native shape views and fall
over. `Foodeez_mark.png` is that image decoded and resampled - it keeps the real alpha channel
the trace encodes. It stays only until a true-vector mark is exported, at which point the
components should import the SVG directly and both this file and the PNG can go.

`Foodeez_full_logo.svg` is a base64 PNG in an SVG wrapper, carrying the same image twice (once
as `href`, once as `xlink:href`) on an opaque white background. Nothing imports it now that
the lockup is composed.

`Foodeez_image.svg` was edited on the way in: it had a `<rect width="100%" height="100%"
fill="#ffffff"/>` behind it, which showed as a white box in dark mode and inside the mobile
welcome screen. The rect was removed and the viewBox tightened to the artwork so the two
assets align when set to a common height.

## Importing

The same import resolves to a different *kind* of thing on each platform, because the two
bundlers treat SVG differently. This is the one thing worth remembering here.

On web, Vite returns a URL string, so the asset goes in an `<img>`:

```ts
import wordmarkUrl from '@foodeez/brand/assets/Foodeez_image.svg';
// <img :src="wordmarkUrl" alt="Foodeez" />
```

On mobile, `react-native-svg-transformer` compiles an SVG into a component that takes its size
as props, while a bitmap import is an opaque handle for `<Image source>`:

```tsx
import Wordmark from '@foodeez/brand/assets/Foodeez_image.svg';
import mark from '@foodeez/brand/assets/Foodeez_mark.png';
// <Wordmark width={200} height={48} />
// <Image source={mark} style={{ width: 54, height: 48 }} resizeMode="contain" />
```

Because an SVG becomes a real component rather than a bitmap, `fill="currentColor"` in the
artwork will not follow React Native text colour - pass an explicit `color`/`fill` prop.

## Adding artwork

Drop the file in `assets/` and import it. No registration step, no rebuild: Vite picks it up
through the alias, and Metro already watches this folder.

Keep the files true vector. An SVG that only wraps a bitmap costs about a third more than the
bitmap alone once base64-encoded, and one that traces a bitmap into per-pixel paths is worse
than either - neither can be recoloured or scaled cleanly.
