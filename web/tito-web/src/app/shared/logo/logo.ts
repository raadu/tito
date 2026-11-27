import { ChangeDetectionStrategy, Component, booleanAttribute, input } from '@angular/core';

/** TITO mark: a stopwatch with a check (time + recorded), optionally with the wordmark. */
@Component({
  selector: 'app-logo',
  template: `
    <svg viewBox="0 0 48 48" [attr.width]="size()" [attr.height]="size()" aria-hidden="true">
      <defs>
        <linearGradient id="tito-logo-gradient" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color="var(--mat-sys-primary)" />
          <stop offset="1" stop-color="var(--mat-sys-tertiary)" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill="url(#tito-logo-gradient)" />
      <rect x="20.5" y="6.5" width="7" height="3.5" rx="1.75" fill="#fff" />
      <circle cx="24" cy="26" r="12.5" fill="none" stroke="#fff" stroke-width="3" />
      <path
        d="M18.5 26.5l3.8 3.8 7.4-7.6"
        fill="none"
        stroke="#fff"
        stroke-width="3"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
    </svg>
    @if (wordmark()) {
      <span class="wordmark">TITO</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: var(--tito-space-3);
    }
    svg {
      flex: none;
      filter: drop-shadow(0 4px 10px color-mix(in srgb, var(--mat-sys-primary) 30%, transparent));
    }
    .wordmark {
      font: var(--mat-sys-title-large);
      font-weight: 700;
      letter-spacing: 0.12em;
      color: var(--mat-sys-on-surface);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'img', '[attr.aria-label]': '"TITO"' },
})
export class Logo {
  readonly size = input(40);
  readonly wordmark = input(false, { transform: booleanAttribute });
}
