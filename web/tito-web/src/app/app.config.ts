import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { problemDetailsInterceptor } from './core/problem-details.interceptor';

// The app's DI registrations (like builder.Services.Add... in Program.cs).
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([problemDetailsInterceptor])),
    provideNativeDateAdapter(), // mat-calendar works with plain JS Date objects
  ],
};
