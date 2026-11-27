import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { Logo } from './shared/logo/logo';

/** The layout around every page: brand bar on top, current route centered below. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, Logo],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
