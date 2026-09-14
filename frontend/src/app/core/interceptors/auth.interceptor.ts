import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from '../state/auth.store';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);
  const token = authStore.getToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: unknown) => {
      // Only force a logout when a token we sent was rejected (expired/invalid
      // session) — not on a plain failed login attempt, which also returns 401
      // but never had a token to begin with.
      if (error instanceof HttpErrorResponse && error.status === 401 && token) {
        authStore.clearSession();
        router.navigate(['/auth/login']);
      }
      return throwError(() => error);
    })
  );
};
