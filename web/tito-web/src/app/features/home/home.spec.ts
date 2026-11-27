import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ABDUL, makeStatus } from '../../../testing/fixtures';
import { AttendanceClient } from '../../api/api-client';
import { AttendanceSession } from '../../core/attendance-session';
import { Home } from './home';

describe('Home', () => {
  const status = vi.fn();

  beforeEach(() => {
    status.mockReset().mockReturnValue(of(makeStatus()));
    TestBed.configureTestingModule({
      imports: [Home],
      providers: [provideRouter([]), { provide: AttendanceClient, useValue: { status } }],
    });
  });

  function render() {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const type = (name: string, value: string) => {
      const input = el.querySelector<HTMLInputElement>(`input[formcontrolname="${name}"]`)!;
      input.value = value;
      input.dispatchEvent(new Event('input')); // tell the reactive form the value changed
    };
    const submit = () => {
      el.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
      fixture.detectChanges();
    };
    return { el, type, submit };
  }

  it('does not call the API when the form is empty', () => {
    const { el, submit } = render();
    submit();

    expect(status).not.toHaveBeenCalled();
    expect(el.textContent).toContain('Employee ID is required.');
  });

  it('trims input, loads status, stores the session and opens /checkin', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const { type, submit } = render();

    type('employeeCode', '  E-1001 ');
    type('fullName', ' Abdul Basit  ');
    submit();

    expect(status).toHaveBeenCalledWith('E-1001', 'Abdul Basit');
    expect(TestBed.inject(AttendanceSession).employee()).toEqual(ABDUL);
    expect(navigate).toHaveBeenCalledWith(['/checkin']);
  });
});
