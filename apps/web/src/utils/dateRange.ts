/**
 * Date helpers for ranges the user picks - a day, or a week.
 *
 * Everything here works in local time and formats by hand. `toISOString().slice(0, 10)` is
 * the usual shortcut and it is wrong for anyone east or west of UTC: it shifts the date by a
 * day for part of every day, which puts the shopping list on the wrong side of midnight.
 */

/** What a picked range covers: a single day, or the Monday-to-Sunday week around it. */
export type RangeMode = 'day' | 'week';

/** yyyy-MM-dd in the reader's own timezone. */
export function toISODate(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Parsed at local midday, so a timezone offset cannot move it to the day before. */
export function fromISODate(value: string): Date {
  return new Date(`${value}T12:00:00`);
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/** The Monday on or before a date. Weeks are planned and shopped Monday to Sunday. */
export function startOfWeek(date: Date): Date {
  const result = new Date(date);
  // getDay() is 0 for Sunday, which belongs to the week that started six days earlier.
  const offset = (result.getDay() + 6) % 7;
  result.setDate(result.getDate() - offset);
  return result;
}

export function formatDay(value: string): string {
  return fromISODate(value).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
}

/** "Mon 3 - Sun 9 Mar", or just the day when a range is one day long. */
export function formatRange(startDate: string, endDate: string): string {
  if (startDate === endDate) {
    return formatDay(startDate);
  }

  const start = fromISODate(startDate);
  const end = fromISODate(endDate);
  const sameMonth = start.getMonth() === end.getMonth() && start.getFullYear() === end.getFullYear();

  const startLabel = start.toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    ...(sameMonth ? {} : { month: 'short' }),
  });
  const endLabel = end.toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
  });

  return `${startLabel} - ${endLabel}`;
}
