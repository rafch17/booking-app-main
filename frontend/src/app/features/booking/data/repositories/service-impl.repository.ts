import { inject, Injectable } from '@angular/core';
import { ServiceRepository } from '../../domain/repositories/service.repository';
import { map, Observable } from 'rxjs';
import ServiceModel from '../../domain/models/service.model';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { ResponseModel } from '../../../auth/domain/models/response.model';
import { ServiceRepositoryMapper } from '../mappers/service.repository.mapper';
import ServiceEntity from '../entities/service.entity';

@Injectable({ providedIn: 'root' })
export class ServiceImplRepository extends ServiceRepository {
  http: HttpClient = inject(HttpClient);
  mapper = new ServiceRepositoryMapper();

  baseUrl: string = environment.apiUrl;
  relativeUrl: string = '/Services';

  override getServices(officeId: number): Observable<ServiceModel[]> {
    // The backend doesn't filter by officeId yet, so it's filtered client-side.
    return this.http
      .get<ResponseModel>(
        this.baseUrl + this.relativeUrl + `?officeId=${officeId}`
      )
      .pipe(
        map((response: ResponseModel) =>
          (response.data as ServiceEntity[])
            .filter((entity) => entity.officeId === officeId)
            .map((entity: ServiceEntity) => this.mapper.mapFrom(entity))
        )
      );
  }
}
