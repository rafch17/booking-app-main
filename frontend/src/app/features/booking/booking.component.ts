import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterOutlet,
} from '@angular/router';
import { OfficeRepository } from './domain/repositories/office.repository';
import { OfficeImplRepository } from './data/repositories/office-impl.repository';
import { GetServicesUseCase } from './domain/use-cases/get-services.usecase';
import { GetWorkingPlacesUseCase } from './domain/use-cases/get-working-places.usecase';
import { ServiceRepository } from './domain/repositories/service.repository';
import { ServiceImplRepository } from './data/repositories/service-impl.repository';
import { BookingStore } from './presentation/state/booking.store';
import { filter, Subscription, switchMap } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SaveBookingUseCase } from './domain/use-cases/save-booking.usecase';
import { BookingRepository } from './domain/repositories/booking.repository';
import { BookingImplRepository } from './data/repositories/booking-impl.repository';
import { GetBookingsByServiceUseCase } from './domain/use-cases/get-bookings-by-service.usecase';
import { AuthStore } from '../../core/state/auth.store';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-booking',
  imports: [RouterOutlet, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary">
      <span>Booking App</span>
      <span style="flex: 1"></span>
      <button mat-icon-button (click)="logout()" matTooltip="Cerrar sesión">
        <mat-icon>logout</mat-icon>
      </button>
    </mat-toolbar>
    <router-outlet></router-outlet>
  `,
  providers: [
    {
      provide: OfficeRepository,
      useClass: OfficeImplRepository,
    },
    GetWorkingPlacesUseCase,
    {
      provide: ServiceRepository,
      useClass: ServiceImplRepository,
    },
    GetServicesUseCase,
    {
      provide: BookingRepository,
      useClass: BookingImplRepository,
    },
    SaveBookingUseCase,
    GetBookingsByServiceUseCase,
    BookingStore,
  ],
})
export class BookingComponent implements OnInit, OnDestroy {
  bookingStore = inject(BookingStore);
  private authStore = inject(AuthStore);
  private suscriptions: Subscription[] = [];
  private _snackbar = inject(MatSnackBar);
  route = inject(ActivatedRoute);
  router = inject(Router);

  logout(): void {
    this.authStore.clearSession();
    this.router.navigate(['/auth/login']);
  }
  ngOnInit(): void {
    this.bookingStore.onLoad();
    const wkplaceSub = this.bookingStore.workingPlaces$
      .pipe(
        filter((list) => list.length > 0),
        switchMap(() => this.route.paramMap)
      )
      .subscribe((params) => {
        console.log('BookingComponent 2 - Retrieved route params:', params);
        const id = params.get('id');
        if (id) {
          try {
            this.bookingStore.setSelectedOfficeId(Number(id));
          } catch (err: any) {
            this._snackbar.open(err.message, '', { duration: 3000 });
            this.router.navigate(['booking', 'working-place']);
          }
        }
      });
    this.suscriptions.push(wkplaceSub);
    const servSub = this.bookingStore.services$
      .pipe(
        filter((s) => s.length > 0),
        switchMap(() => this.route.paramMap)
      )
      .subscribe((params) => {
        const sId = params.get('sId');
        console.log(
          'ServiceDetailComponent - Retrieved sId from route params:',
          sId
        );
        if (sId) {
          try {
            this.bookingStore.setServiceId(Number(sId));
          } catch (err: any) {
            this._snackbar.open(err.message, '', { duration: 3000 });
            this.router.navigate(['booking', 'working-place']);
          }
        }
      });
    this.suscriptions.push(servSub);
  }
  ngOnDestroy(): void {
    if (this.suscriptions.length > 0) {
      this.suscriptions.forEach((s) => s.unsubscribe());
    }
  }
}
