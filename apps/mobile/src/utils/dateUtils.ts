import {
  format,
  isToday as dateFnsIsToday,
  startOfWeek,
  addDays,
  getHours,
} from 'date-fns';

/**
 * Format date for display: "Wednesday, May 21"
 */
export function formatDisplayDate(date: Date): string {
  return format(date, 'EEEE, MMMM d');
}

/**
 * Format date for short display: "May 21"
 */
export function formatShortDate(date: Date): string {
  return format(date, 'MMM d');
}

/**
 * Format date for API calls: "2025-05-21"
 */
export function formatApiDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

/**
 * Get array of 7 dates for the week containing referenceDate (Sunday-start).
 */
export function getWeekDays(referenceDate: Date): Date[] {
  const start = startOfWeek(referenceDate, { weekStartsOn: 0 });
  return Array.from({ length: 7 }, (_, i) => addDays(start, i));
}

/**
 * Check whether a date is today.
 */
export function isToday(date: Date): boolean {
  return dateFnsIsToday(date);
}

/**
 * Return time-of-day greeting string.
 */
export function getGreeting(): 'morning' | 'afternoon' | 'evening' {
  const hour = getHours(new Date());
  if (hour < 12) return 'morning';
  if (hour < 17) return 'afternoon';
  return 'evening';
}

/**
 * Format a date as day-of-month number: "21"
 */
export function formatDayNumber(date: Date): string {
  return format(date, 'd');
}

/**
 * Format a date as short day name: "Wed"
 */
export function formatDayName(date: Date): string {
  return format(date, 'EEE');
}

/**
 * Parse an ISO date string to a Date object (at midnight local time).
 */
export function parseApiDate(dateStr: string): Date {
  const [year, month, day] = dateStr.split('-').map(Number);
  return new Date(year, month - 1, day);
}
