/** Milliseconds to add to the device clock to get server time (positive = device is behind). */
export function clockOffsetMs(serverNowUtc: string, deviceNowMs: number): number {
  return Date.parse(serverNowUtc) - deviceNowMs;
}

/** Whole seconds from an ISO UTC timestamp to `untilMs` (epoch milliseconds). Never negative. */
export function secondsBetween(fromUtc: string, untilMs: number): number {
  return Math.max(0, Math.floor((untilMs - Date.parse(fromUtc)) / 1000));
}

/** 12_725 -> "03:32:05". */
export function formatDuration(totalSeconds: number): string {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  return [hours, minutes, seconds].map((n) => String(n).padStart(2, '0')).join(':');
}

/** "2026-11-27" -> a local Date at midnight (what mat-calendar expects). */
export function parseDateOnly(isoDate: string): Date {
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** A local Date -> "2026-11-27" (the JSON shape of C# DateOnly). */
export function formatDateOnly(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
