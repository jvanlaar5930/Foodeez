/**
 * The purple accent used for AI analysis panels (meal score, day analysis). Kept separate
 * from the main Palette because it is only ever used for these panels, but it still needs a
 * light and dark version - a panel background hardcoded to the light value read as a white
 * card with barely-visible light-grey text once the rest of the app went dark, since only
 * the text color (from the real Palette) was following the theme and the background was not.
 */
export interface AIPanelColors {
  panelBg: string;
  panelBorder: string;
  /** Section labels and headings - "AI Day Analysis", "Meal Score". */
  accentText: string;
  buttonBorder: string;
  ringTrack: string;
  tagBg: string;
  tagText: string;
}

export const AIPanelLight: AIPanelColors = {
  panelBg: '#FAF5FF',
  panelBorder: '#DDD6FE',
  accentText: '#7C3AED',
  buttonBorder: '#C4B5FD',
  ringTrack: '#E9D5FF',
  tagBg: '#FEE2E2',
  tagText: '#B91C1C',
};

export const AIPanelDark: AIPanelColors = {
  panelBg: '#241A38',
  panelBorder: '#4C3575',
  accentText: '#C4B5FD',
  buttonBorder: '#5B3B8C',
  ringTrack: '#3D2A5C',
  tagBg: '#4A1620',
  tagText: '#FCA5A5',
};

/**
 * The amber accent used for recipes the assistant wrote into a reply.
 *
 * Same reason as the purple pair above, and the same bug: this card was sixteen hardcoded
 * light-mode values, so a dark thread had a cream card with a white inner panel sitting in
 * the middle of it - the one panel in the app that did not follow the theme.
 */
export interface RecipePanelColors {
  panelBg: string;
  panelBorder: string;
  /** The "Recipes" label, and the confirmation line under the button. */
  accentText: string;
  /** The card each recipe sits on, inside the panel. */
  itemBg: string;
  itemDivider: string;
  title: string;
  body: string;
  meta: string;
  buttonBg: string;
}

export const RecipePanelLight: RecipePanelColors = {
  panelBg: '#FFFBEB',
  panelBorder: '#FCD34D',
  accentText: '#B45309',
  itemBg: '#FFFFFF',
  itemDivider: '#FDE68A',
  title: '#1F2937',
  body: '#4B5563',
  meta: '#6B7280',
  buttonBg: '#D97706',
};

export const RecipePanelDark: RecipePanelColors = {
  panelBg: '#2A2010',
  panelBorder: '#6B4E16',
  accentText: '#FCD34D',
  itemBg: '#1F1A0F',
  itemDivider: '#4A3812',
  title: '#F3F0E8',
  body: '#C9C3B6',
  meta: '#9C968A',
  buttonBg: '#B45309',
};
