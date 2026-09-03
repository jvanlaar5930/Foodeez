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
