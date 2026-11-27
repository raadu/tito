import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AttendanceClient } from '../../api/api-client';
import { AttendanceSession } from '../../core/attendance-session';
import { Button } from '../../shared/button/button';

/** Same limits as Employee.CodeMaxLength / NameMaxLength in Tito.Core. */
export const CODE_MAX_LENGTH = 20;
export const NAME_MAX_LENGTH = 100;

@Component({
  selector: 'app-home',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, Button],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush, // re-render only when signals/inputs change
})
export class Home {
  private readonly attendance = inject(AttendanceClient);
  private readonly session = inject(AttendanceSession);
  private readonly router = inject(Router);

  protected readonly codeMax = CODE_MAX_LENGTH;
  protected readonly nameMax = NAME_MAX_LENGTH;
  protected readonly loading = signal(false);

  // Pre-fill when coming back from /checkin so the user doesn't retype.
  private readonly previous = this.session.employee();
  protected readonly form = inject(NonNullableFormBuilder).group({
    employeeCode: [this.previous?.employeeCode ?? '', [Validators.required, Validators.maxLength(CODE_MAX_LENGTH)]],
    fullName: [this.previous?.fullName ?? '', [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched(); // show the red error messages
      return;
    }

    const { employeeCode, fullName } = this.form.getRawValue();
    const employee = { employeeCode: employeeCode.trim(), fullName: fullName.trim() };

    this.loading.set(true);
    this.attendance
      .status(employee.employeeCode, employee.fullName)
      .pipe(finalize(() => this.loading.set(false))) // runs on success AND error, like `finally`
      .subscribe({
        next: (status) => {
          this.session.start(employee, status);
          void this.router.navigate(['/checkin']);
        },
        error: () => {}, // the interceptor already showed the message (e.g. 409 name mismatch)
      });
  }
}
