import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AttendanceClient, AttendanceRequest, AttendanceStatus, AttendanceStatusResponse } from '../../api/api-client';
import { AttendanceSession } from '../../core/attendance-session';
import { formatDateOnly, formatDuration, parseDateOnly, secondsBetween } from '../../core/worked-time';
import { Button } from '../../shared/button/button';

/** Same limit as AttendanceRecord.CommentMaxLength in Tito.Core. */
export const COMMENT_MAX_LENGTH = 500;
/** How long the success screen stays before returning to Home. */
export const REDIRECT_SECONDS = 60;

type Step = 'pick-date' | 'details' | 'done'; // a union of string literals: like a tiny enum
type Action = 'time-in' | 'time-out';

const STATUS_LABELS: Record<AttendanceStatus, string> = {
  [AttendanceStatus.Absent]: 'Not in yet',
  [AttendanceStatus.Working]: 'Working',
  [AttendanceStatus.Done]: 'Done today',
};

/** Already timed in? Skip the calendar. Already done? Show the summary. */
function firstStep(status: AttendanceStatus): Step {
  switch (status) {
    case AttendanceStatus.Working:
      return 'details';
    case AttendanceStatus.Done:
      return 'done';
    default:
      return 'pick-date';
  }
}

@Component({
  selector: 'app-checkin',
  imports: [DatePipe, ReactiveFormsModule, MatDatepickerModule, MatFormFieldModule, MatInputModule, Button],
  templateUrl: './checkin.html',
  styleUrl: './checkin.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Checkin {
  private readonly attendance = inject(AttendanceClient);
  private readonly session = inject(AttendanceSession);
  private readonly router = inject(Router);

  // Local copies taken once: the page keeps rendering correctly even after the shared
  // session is cleared on the way back Home. The route guard guarantees they exist (`!`).
  protected readonly employee = this.session.employee()!;
  protected readonly status = signal<AttendanceStatusResponse>(this.session.status()!);
  private readonly clockOffsetMs = this.session.clockOffsetMs();

  protected readonly initials = this.employee.fullName
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
  protected readonly statusLabel = computed(() => STATUS_LABELS[this.status().status]);

  /** "Today" comes from the server (company time zone), not from the device. */
  protected readonly today = parseDateOnly(this.status().today);
  protected readonly selectedDate = signal<Date | null>(this.today);

  protected readonly step = signal<Step>(firstStep(this.status().status));
  protected readonly lastAction = signal<Action | null>(null);
  protected readonly submitting = signal(false);

  /** Working (timed in, not out yet) -> the button says Time Out. */
  protected readonly isTimeOut = computed(() => this.status().status === AttendanceStatus.Working);

  // One clock signal, ticking every second. Everything computed from it updates live.
  private readonly now = signal(Date.now());

  protected readonly workedSoFar = computed(() => {
    const timeIn = this.status().timeInUtc;
    return formatDuration(timeIn ? secondsBetween(timeIn, this.now() + this.clockOffsetMs) : 0);
  });

  protected readonly totalWorked = computed(() => {
    const { timeInUtc, timeOutUtc } = this.status(); // object destructuring
    return formatDuration(timeInUtc && timeOutUtc ? secondsBetween(timeInUtc, Date.parse(timeOutUtc)) : 0);
  });

  // ---- Auto-redirect on the done screen ----
  private readonly redirectAt = signal<number | null>(null);
  protected readonly secondsLeft = computed(() => {
    const at = this.redirectAt();
    return at === null ? REDIRECT_SECONDS : Math.max(0, Math.ceil((at - this.now()) / 1000));
  });
  protected readonly redirectPercent = computed(() => (this.secondsLeft() / REDIRECT_SECONDS) * 100);

  protected readonly commentMax = COMMENT_MAX_LENGTH;
  protected readonly comment = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(COMMENT_MAX_LENGTH)],
  });

  /** mat-calendar asks this for every day it draws; only today is selectable. */
  protected readonly onlyToday = (date: Date | null): boolean =>
    date !== null && formatDateOnly(date) === this.status().today;

  constructor() {
    if (this.step() === 'done') this.startRedirect();

    const timer = setInterval(() => this.tick(), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer)); // stop the timer when the page closes
  }

  protected selectDate(): void {
    if (this.selectedDate()) this.step.set('details');
  }

  /** Back from the details step: to the calendar, or Home if we skipped the calendar. */
  protected back(): void {
    if (this.isTimeOut()) this.changeEmployee();
    else this.step.set('pick-date');
  }

  protected submit(): void {
    const date = this.selectedDate();
    if (!date || this.comment.invalid) return;

    const request: AttendanceRequest = {
      ...this.employee, // spread: copies employeeCode and fullName into this object
      date: formatDateOnly(date),
      comment: this.comment.value.trim() || null,
    };
    const action: Action = this.isTimeOut() ? 'time-out' : 'time-in';
    const call = action === 'time-out' ? this.attendance.timeOut(request) : this.attendance.timeIn(request);

    this.submitting.set(true);
    call.pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: (status) => {
        this.status.set(status);
        this.lastAction.set(action);
        this.step.set('done');
        this.startRedirect();
      },
      error: () => {}, // the interceptor shows the ProblemDetails message
    });
  }

  protected backToHome(): void {
    this.redirectAt.set(null); // never navigate twice
    this.session.clear(); // next person gets an empty form
    void this.router.navigate(['/']);
  }

  protected changeEmployee(): void {
    void this.router.navigate(['/']); // keeps the session so Home is pre-filled
  }

  private startRedirect(): void {
    this.now.set(Date.now());
    this.redirectAt.set(Date.now() + REDIRECT_SECONDS * 1000);
  }

  private tick(): void {
    this.now.set(Date.now());
    const at = this.redirectAt();
    if (at !== null && this.now() >= at) this.backToHome();
  }
}
