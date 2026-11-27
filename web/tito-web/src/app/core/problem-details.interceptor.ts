import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

/** The parts of an RFC 7807 ProblemDetails body we display. */
interface Problem {
  title?: string | null;
  detail?: string | null;
  errors?: Record<string, string[]>; // ASP.NET Core validation errors: { "FullName": ["..."] }
}

/** Turns an error status + parsed body into one human-readable line. */
export function problemMessage(status: number, body: unknown): string {
  if (status === 0) return 'Cannot reach the server. Check your connection and try again.';
  if (status === 429) return 'Too many requests. Please wait a minute and try again.';

  const problem = (typeof body === 'object' && body !== null ? body : {}) as Problem;
  const firstValidationError = Object.values(problem.errors ?? {}).flat()[0];

  return firstValidationError ?? problem.detail ?? problem.title ?? `Request failed (${status}).`;
}

/** Reads the error body whether it arrived as a Blob (NSwag client), a string or an object. */
async function readBody(error: HttpErrorResponse): Promise<unknown> {
  const raw: unknown = error.error instanceof Blob ? await error.error.text() : error.error;
  if (typeof raw !== 'string') return raw;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

/** Shows every failed API call's ProblemDetails message in a snackbar, then rethrows. */
export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        void readBody(error).then((body) =>
          snackBar.open(problemMessage(error.status, body), 'Close', { duration: 6000 }),
        );
      }
      return throwError(() => error); // keep the error flowing to the caller
    }),
  );
};
