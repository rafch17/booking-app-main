import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthStore } from '../state/auth.store';

@Injectable({ providedIn: 'root' })
export class GuestGuard implements CanActivate {
  constructor(private auth: AuthStore, private router: Router) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.auth.isLoggedIn$.pipe(
      map(isLoggedIn => {
        if (isLoggedIn) {
          return this.router.createUrlTree(['/booking/working-place']);
        }
        return true;
      })
    );
  }
}
