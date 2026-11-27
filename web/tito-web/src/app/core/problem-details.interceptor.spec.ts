import { problemMessage } from './problem-details.interceptor';

describe('problemMessage', () => {
  it('prefers the first validation error', () => {
    const body = { title: 'One or more validation errors occurred.', errors: { FullName: ['Full name is required.'] } };
    expect(problemMessage(400, body)).toBe('Full name is required.');
  });

  it('uses detail for domain errors', () => {
    const body = { title: 'Conflict', detail: 'You have already timed in today.' };
    expect(problemMessage(409, body)).toBe('You have already timed in today.');
  });

  it('explains network failures and rate limiting', () => {
    expect(problemMessage(0, null)).toContain('Cannot reach the server');
    expect(problemMessage(429, null)).toContain('Too many requests');
  });

  it('falls back to the status code', () => {
    expect(problemMessage(500, null)).toBe('Request failed (500).');
  });
});
