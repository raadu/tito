import { Injectable, computed, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AttendanceStatusResponse } from '../api/api-client';
import { clockOffsetMs } from './worked-time';

/** Who is checking in, and their status for today. Shared by the Home and Check-in pages. */
export interface Employee {
  employeeCode: string;
  fullName: string;
}

@Injectable({ providedIn: 'root' }) // one instance for the whole app (like AddSingleton)
export class AttendanceSession {
  // Private writable signals; components only get read-only views.
  private readonly _employee = signal<Employee | null>(null);
  private readonly _status = signal<AttendanceStatusResponse | null>(null);
  private readonly _clockOffsetMs = signal(0);

  readonly employee = this._employee.asReadonly();
  readonly status = this._status.asReadonly();
  readonly clockOffsetMs = this._clockOffsetMs.asReadonly();
  readonly hasEmployee = computed(() => this._employee() !== null);

  start(employee: Employee, status: AttendanceStatusResponse): void {
    this._employee.set(employee);
    this.update(status);
  }

  /** Store a fresh status from the API and re-measure the device clock error. */
  update(status: AttendanceStatusResponse): void {
    this._status.set(status);
    this._clockOffsetMs.set(clockOffsetMs(status.serverNowUtc, Date.now()));
  }

  clear(): void {
    this._employee.set(null);
    this._status.set(null);
  }
}

/** Route guard: /checkin only makes sense after the Home form. Otherwise go back to "/". */
export const requireEmployee: CanActivateFn = () =>
  inject(AttendanceSession).hasEmployee() || inject(Router).createUrlTree(['/']);
