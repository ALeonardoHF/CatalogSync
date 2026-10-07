import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';

export interface SolicitudDto {
  id: string;
  libroId: string;
  isbn: string;
  titulo: string;
  autor: string;
  existencia?: number;
  estado: string;
  creadoEn: string;
  enviadoEn?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificacionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly base = `${this.apiUrl}/api/notificaciones`;

  getMisSolicitudes(): Observable<SolicitudDto[]> {
    return this.http.get<SolicitudDto[]>(this.base);
  }

  solicitar(libroId: string): Observable<unknown> {
    return this.http.post(`${this.base}/${libroId}`, {});
  }

  cancelar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
