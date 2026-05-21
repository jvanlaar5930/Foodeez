import { computed, type Ref } from 'vue';
import {
  startOfWeek,
  addDays,
  subDays,
  format,
  isToday,
  isSameDay,
} from 'date-fns';

export interface CalendarDay {
  date: Date;
  dateString: string;
  dayAbbr: string;
  dayNumber: string;
  isToday: boolean;
}

export function useCalendar(referenceDate: Ref<Date>) {
  const weekDays = computed<CalendarDay[]>(() => {
    const weekStart = startOfWeek(referenceDate.value, { weekStartsOn: 1 }); // Monday start
    return Array.from({ length: 7 }, (_, i) => {
      const date = addDays(weekStart, i);
      return {
        date,
        dateString: format(date, 'yyyy-MM-dd'),
        dayAbbr: format(date, 'EEE'),
        dayNumber: format(date, 'd'),
        isToday: isToday(date),
      };
    });
  });

  const weekLabel = computed(() => {
    const days = weekDays.value;
    const first = days[0].date;
    const last = days[6].date;
    if (first.getMonth() === last.getMonth()) {
      return format(first, 'MMMM yyyy');
    }
    return `${format(first, 'MMM')} – ${format(last, 'MMM yyyy')}`;
  });

  function prevWeek(): void {
    referenceDate.value = subDays(referenceDate.value, 7);
  }

  function nextWeek(): void {
    referenceDate.value = addDays(referenceDate.value, 7);
  }

  function goToToday(): void {
    referenceDate.value = new Date();
  }

  function isSelectedDay(date: Date): boolean {
    return isSameDay(date, referenceDate.value);
  }

  return { weekDays, weekLabel, prevWeek, nextWeek, goToToday, isSelectedDay };
}
