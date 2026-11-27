import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ABDUL, makeStatus } from '../../../testing/fixtures';
import { AttendanceClient, AttendanceStatus, AttendanceStatusResponse } from '../../api/api-client';
import { AttendanceSession } from '../../core/attendance-session';
import { Checkin } from './checkin';

describe('Checkin', () => {
  const timeIn = vi.fn();
  const timeOut = vi.fn();

  function render(status: AttendanceStatusResponse) {
    TestBed.configureTestingModule({
      imports: [Checkin],
      providers: [
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: AttendanceClient, useValue: { timeIn, timeOut } },
      ],
    });
    TestBed.inject(AttendanceSession).start(ABDUL, status);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const fixture = TestBed.createComponent(Checkin);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const text = (testId: string) => el.querySelector(`[data-testid="${testId}"]`)?.textContent ?? null;
    const click = (label: string) => {
      const button = [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === label);
      if (!button) throw new Error(`No button "${label}"`);
      button.click();
      fixture.detectChanges();
    };
    return { el, fixture, navigate, text, click };
  }

  beforeEach(() => {
    timeIn.mockReset();
    timeOut.mockReset();
  });

  afterEach(() => vi.useRealTimers());

  it('asks for the date, then times in when the employee is absent', () => {
    timeIn.mockReturnValue(of(makeStatus({ status: AttendanceStatus.Working, timeInUtc: '2026-11-27T08:00:00Z' })));
    const { el, text, click } = render(makeStatus());

    expect(el.querySelector('mat-calendar')).not.toBeNull();
    click('Select Date');
    expect(text('worked-so-far')).toBeNull();
    click('Time In');

    expect(timeIn).toHaveBeenCalledWith({ ...ABDUL, date: '2026-11-27', comment: null });
    expect(text('done')).toContain('Time In recorded');
    expect(text('redirect')).toContain('redirected to the home page in 60 seconds');
  });

  it('skips the date for someone already timed in and shows a live HH:MM:SS counter', () => {
    // Device clock frozen at the server time: 12:00:00 UTC. Abdul timed in at 08:30:00.
    vi.useFakeTimers({ now: Date.parse('2026-11-27T12:00:00Z') });
    const working = makeStatus({ status: AttendanceStatus.Working, timeInUtc: '2026-11-27T08:30:00Z' });
    const { el, fixture, text } = render(working);

    expect(el.querySelector('mat-calendar')).toBeNull();
    expect(text('worked-so-far')).toContain('03:30:00');

    vi.advanceTimersByTime(5_000); // five 1-second ticks
    fixture.detectChanges();
    expect(text('worked-so-far')).toContain('03:30:05');
  });

  it('times out and shows the total worked', () => {
    const working = makeStatus({ status: AttendanceStatus.Working, timeInUtc: '2026-11-27T08:30:00Z' });
    timeOut.mockReturnValue(of({ ...working, status: AttendanceStatus.Done, timeOutUtc: '2026-11-27T12:00:15Z' }));
    const { text, click } = render(working);

    click('Time Out');

    expect(timeOut).toHaveBeenCalled();
    expect(text('done')).toContain('Time Out recorded');
    expect(text('total-worked')).toBe('03:30:15');
  });

  it('counts down and returns Home after 60 seconds', () => {
    vi.useFakeTimers({ now: Date.parse('2026-11-27T16:00:00Z') });
    const { fixture, navigate, text } = render(
      makeStatus({
        status: AttendanceStatus.Done,
        timeInUtc: '2026-11-27T08:00:00Z',
        timeOutUtc: '2026-11-27T16:00:00Z',
      }),
    );
    expect(text('done')).toContain("You're done for today");

    vi.advanceTimersByTime(8_000);
    fixture.detectChanges();
    expect(text('redirect')).toContain('in 52 seconds');
    expect(navigate).not.toHaveBeenCalled();

    vi.advanceTimersByTime(52_000);
    expect(navigate).toHaveBeenCalledWith(['/']);
    expect(TestBed.inject(AttendanceSession).employee()).toBeNull(); // next person gets an empty form
  });
});
