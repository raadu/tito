import { AttendanceStatus, AttendanceStatusResponse } from '../app/api/api-client';

/** A status response for tests; override only the fields a test cares about. */
export function makeStatus(overrides: Partial<AttendanceStatusResponse> = {}): AttendanceStatusResponse {
  return {
    employeeCode: 'E-1001',
    fullName: 'Abdul Basit',
    status: AttendanceStatus.Absent,
    today: '2026-11-27',
    timeInUtc: null,
    timeInComment: null,
    timeOutUtc: null,
    timeOutComment: null,
    workedMinutes: 0,
    serverNowUtc: '2026-11-27T12:00:00Z',
    ...overrides,
  };
}

export const ABDUL = { employeeCode: 'E-1001', fullName: 'Abdul Basit' };
