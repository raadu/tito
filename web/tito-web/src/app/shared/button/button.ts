import { ChangeDetectionStrategy, Component, booleanAttribute, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost';

/**
 * The app's one button style. Attach it to a native element so all native behaviour stays:
 *   <button appButton type="submit" [loading]="saving()">Save</button>
 *   <button appButton variant="secondary" (click)="back()">Back</button>
 */
@Component({
  selector: 'button[appButton], a[appButton]',
  template: `
    <span class="label"><ng-content /></span>
    @if (loading()) {
      <span class="spinner" aria-hidden="true"></span>
    }
  `,
  styleUrl: './button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // `host` binds attributes/classes on the <button> element itself.
  host: {
    '[attr.data-variant]': 'variant()',
    '[class.is-loading]': 'loading()',
    '[attr.aria-busy]': 'loading() || null', // null removes the attribute
  },
})
export class Button {
  readonly variant = input<ButtonVariant>('primary');
  readonly loading = input(false, { transform: booleanAttribute }); // allows plain `loading`
}
