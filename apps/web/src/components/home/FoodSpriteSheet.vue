<!--
  A single hidden SVG holding every decorative food shape as a <symbol>, so the parallax
  backdrop can stamp them with <use> instead of repeating path data per instance.

  Each symbol draws in `currentColor` only, using opacity to suggest interior detail. That
  keeps a shape legible as a single tinted silhouette at low contrast, and lets the caller
  pick the hue (and a dark-mode hue) with ordinary text-colour classes.

  Mount once per page - the symbol ids are global.
-->
<template>
  <svg width="0" height="0" class="absolute" aria-hidden="true" focusable="false">
    <defs>
      <!-- Ramen bowl with steam and chopsticks -->
      <symbol id="pf-ramen" viewBox="0 0 64 64">
        <g fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" opacity=".55">
          <path d="M23 25c0-4 4-5 4-9s-4-5-4-9" />
          <path d="M32 25c0-4 4-5 4-9s-4-5-4-9" />
          <path d="M41 25c0-4 4-5 4-9s-4-5-4-9" />
        </g>
        <g stroke="currentColor" stroke-width="2.5" stroke-linecap="round" opacity=".7">
          <path d="M44 27 62 13" />
          <path d="M47 31 64 18" />
        </g>
        <path d="M7 33h50a25 25 0 0 1-50 0z" fill="currentColor" opacity=".4" />
        <rect x="4" y="29" width="56" height="5.5" rx="2.75" fill="currentColor" opacity=".85" />
      </symbol>

      <!-- Poke / grain bowl -->
      <symbol id="pf-bowl" viewBox="0 0 64 64">
        <circle cx="21" cy="25" r="7" fill="currentColor" opacity=".75" />
        <circle cx="34" cy="21" r="8" fill="currentColor" opacity=".55" />
        <circle cx="46" cy="26" r="6" fill="currentColor" opacity=".8" />
        <path d="M7 33h50a25 25 0 0 1-50 0z" fill="currentColor" opacity=".4" />
        <rect x="5" y="29" width="54" height="5.5" rx="2.75" fill="currentColor" opacity=".85" />
      </symbol>

      <!-- Pizza slice -->
      <symbol id="pf-pizza" viewBox="0 0 64 64">
        <path d="M32 5 55 51a3 3 0 0 1-2.7 4.4H11.7A3 3 0 0 1 9 51L32 5z" fill="currentColor" opacity=".33" />
        <path d="M13.4 47h37.2l2.2 4.4A3 3 0 0 1 50.2 56H13.8a3 3 0 0 1-2.6-4.6L13.4 47z" fill="currentColor" opacity=".7" />
        <g fill="currentColor" opacity=".8">
          <circle cx="27" cy="30" r="3.4" />
          <circle cx="37" cy="38" r="3.4" />
          <circle cx="32" cy="19" r="2.8" />
          <circle cx="23" cy="41" r="2.8" />
        </g>
      </symbol>

      <!-- Sushi nigiri: the fish has to dome and overhang the rice to read as a topping -->
      <symbol id="pf-sushi" viewBox="0 0 64 64">
        <rect x="10" y="34" width="44" height="18" rx="9" fill="currentColor" opacity=".33" />
        <path d="M6 33c0-8.8 11.6-16 26-16s26 7.2 26 16a3 3 0 0 1-3 3H9a3 3 0 0 1-3-3z" fill="currentColor" opacity=".75" />
        <rect x="26.5" y="15" width="11" height="37" rx="2.5" fill="currentColor" opacity=".95" />
      </symbol>

      <!-- Burger: a domed bun keeps this distinct from the two round-bottomed bowls -->
      <symbol id="pf-burger" viewBox="0 0 64 64">
        <path d="M6 28c0-11 11.6-19 26-19s26 8 26 19a3 3 0 0 1-3 3H9a3 3 0 0 1-3-3z" fill="currentColor" opacity=".45" />
        <g fill="currentColor" opacity=".8">
          <ellipse cx="24" cy="19" rx="2.6" ry="1.6" transform="rotate(-20 24 19)" />
          <ellipse cx="34" cy="16" rx="2.6" ry="1.6" transform="rotate(10 34 16)" />
          <ellipse cx="43" cy="21" rx="2.6" ry="1.6" transform="rotate(25 43 21)" />
        </g>
        <g fill="currentColor" opacity=".75">
          <rect x="6" y="31" width="52" height="4" rx="2" />
          <circle cx="14" cy="36" r="4" />
          <circle cx="25" cy="36" r="4" />
          <circle cx="36" cy="36" r="4" />
          <circle cx="47" cy="36" r="4" />
          <circle cx="56" cy="35.5" r="3.5" />
        </g>
        <rect x="7" y="40" width="50" height="8" rx="4" fill="currentColor" opacity=".95" />
        <path d="M9 48h46a3 3 0 0 1 3 3c0 4-3.2 7-7 7H13c-3.8 0-7-3-7-7a3 3 0 0 1 3-3z" fill="currentColor" opacity=".45" />
      </symbol>

      <!-- Pancake stack with butter -->
      <symbol id="pf-pancakes" viewBox="0 0 64 64">
        <ellipse cx="32" cy="47" rx="24" ry="7.5" fill="currentColor" opacity=".35" />
        <ellipse cx="32" cy="38" rx="24" ry="7.5" fill="currentColor" opacity=".45" />
        <ellipse cx="32" cy="29" rx="24" ry="7.5" fill="currentColor" opacity=".55" />
        <rect x="25" y="15" width="14" height="9" rx="2" fill="currentColor" opacity=".9" />
      </symbol>

      <!-- Avocado half: narrow neck flaring into a round bulb, pit centred in the bulb -->
      <symbol id="pf-avocado" viewBox="0 0 64 64">
        <path
          d="M32 5c-6 0-9 6-9 13 0 6-9 10-9 22a18 18 0 0 0 36 0c0-12-9-16-9-22 0-7-3-13-9-13z"
          fill="currentColor"
          opacity=".33"
        />
        <circle cx="32" cy="41" r="9" fill="currentColor" opacity=".85" />
      </symbol>

      <!-- Citrus slice -->
      <symbol id="pf-citrus" viewBox="0 0 64 64">
        <circle cx="32" cy="32" r="26" fill="currentColor" opacity=".3" />
        <circle cx="32" cy="32" r="20.5" fill="currentColor" opacity=".28" />
        <g stroke="currentColor" stroke-width="2" opacity=".75">
          <path d="M32 11.5v41M11.5 32h41M17.5 17.5l29 29M46.5 17.5l-29 29" />
        </g>
      </symbol>

      <!-- Tomato -->
      <symbol id="pf-tomato" viewBox="0 0 64 64">
        <circle cx="32" cy="38" r="21" fill="currentColor" opacity=".4" />
        <g fill="currentColor" opacity=".85">
          <ellipse cx="23" cy="17" rx="8.5" ry="3.5" transform="rotate(-25 23 17)" />
          <ellipse cx="41" cy="17" rx="8.5" ry="3.5" transform="rotate(25 41 17)" />
          <rect x="30.25" y="8" width="3.5" height="11" rx="1.75" />
        </g>
      </symbol>

      <!-- Chilli pepper -->
      <symbol id="pf-chilli" viewBox="0 0 64 64">
        <path
          d="M22 16c8.5-1.5 15 3 19 9.5 5 8.5 5.5 19 1 27-3.5 6.5-10 9.5-15 7-4.5-2.5-5.5-8-3-12.5 3.5-6.5 9-9.5 11-16 1.5-5-.5-11-4.5-14z"
          fill="currentColor"
          opacity=".38"
        />
        <path
          d="M22 16c-3-4.5-8.5-6.5-13-5.5"
          fill="none"
          stroke="currentColor"
          stroke-width="3.5"
          stroke-linecap="round"
          opacity=".85"
        />
      </symbol>

      <!-- Herb leaf -->
      <symbol id="pf-leaf" viewBox="0 0 64 64">
        <path
          d="M55 8C30 8 11 23 11 44c0 4.4 1 8.4 2.4 11 15-2.5 40-13 41.6-47z"
          fill="currentColor"
          opacity=".38"
        />
        <path
          d="M52 11C37.5 21.5 23 34 14.5 53"
          fill="none"
          stroke="currentColor"
          stroke-width="2.5"
          stroke-linecap="round"
          opacity=".85"
        />
      </symbol>

      <!-- Wheat sprig -->
      <symbol id="pf-wheat" viewBox="0 0 64 64">
        <path
          d="M32 60V20"
          fill="none"
          stroke="currentColor"
          stroke-width="3"
          stroke-linecap="round"
          opacity=".85"
        />
        <g fill="currentColor" opacity=".4">
          <ellipse cx="32" cy="11" rx="4.5" ry="8.5" />
          <ellipse cx="22" cy="22" rx="4.5" ry="8.5" transform="rotate(-32 22 22)" />
          <ellipse cx="42" cy="22" rx="4.5" ry="8.5" transform="rotate(32 42 22)" />
          <ellipse cx="22" cy="35" rx="4.5" ry="8.5" transform="rotate(-32 22 35)" />
          <ellipse cx="42" cy="35" rx="4.5" ry="8.5" transform="rotate(32 42 35)" />
        </g>
      </symbol>

      <!-- Fried egg -->
      <symbol id="pf-egg" viewBox="0 0 64 64">
        <path
          d="M19 11c10.5-5 23-2.5 29 5s4.5 17-2 22c-4 3-4 8.5-9.5 12.5-6.5 5-17 4-22-2.5s-4-13.5.5-19c3.5-4.5 1.5-14 4-18z"
          fill="currentColor"
          opacity=".3"
        />
        <circle cx="30" cy="30" r="9.5" fill="currentColor" opacity=".85" />
      </symbol>

      <!-- Croissant -->
      <symbol id="pf-croissant" viewBox="0 0 64 64">
        <path
          d="M6 42c0-14.9 11.6-27 26-27s26 12.1 26 27c0 3.4-3.4 5.6-6.5 4.3-4.4-1.9-7.6-7.4-13-9.6-4.2-1.7-8.8-1.7-13 0-5.4 2.2-8.6 7.7-13 9.6C9.4 47.6 6 45.4 6 42z"
          fill="currentColor"
          opacity=".4"
        />
        <g fill="none" stroke="currentColor" stroke-width="2" opacity=".7">
          <path d="M21 23.5v12M32 20v13M43 23.5v12" />
        </g>
      </symbol>
    </defs>
  </svg>
</template>
