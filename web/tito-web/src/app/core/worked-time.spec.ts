import { clockOffsetMs, formatDateOnly, formatDuration, parseDateOnly, secondsBetween } from './worked-time';

describe('worked-time', () => {
  it('formats seconds as HH:MM:SS', () => {
    expect(formatDuration(0)).toBe('00:00:00');
    expect(formatDuration(61)).toBe('00:01:01');
    expect(formatDuration(12_725)).toBe('03:32:05');
  });

  it('corrects a device clock that is 5 minutes slow', () => {
    const deviceNow = Date.parse('2026-11-27T09:55:00Z');
    const offset = clockOffsetMs('2026-11-27T10:00:00Z', deviceNow);

    // Timed in at 08:00 server time; the device shows 09:55, but 2h worked is correct.
    expect(secondsBetween('2026-11-27T08:00:00Z', deviceNow + offset)).toBe(7200);
  });

  it('never returns negative seconds', () => {
    expect(secondsBetween('2026-11-27T10:00:00Z', Date.parse('2026-11-27T09:00:00Z'))).toBe(0);
  });

  it('parses a DateOnly string as local midnight', () => {
    const d = parseDateOnly('2026-11-27');
    expect([d.getFullYear(), d.getMonth(), d.getDate(), d.getHours()]).toEqual([2026, 10, 27, 0]);
  });

  it('round-trips a DateOnly string', () => {
    expect(formatDateOnly(parseDateOnly('2026-03-05'))).toBe('2026-03-05');
  });
});
