import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';

export interface FavoritoDto {
  id: string;
  libroId: string;
  isbn: string;
  titulo: string;
  autor: string;
  editorial: string;
  precioVenta: number;
  existencia?: number;
  estadoInventario?: string;
  portada?: string;
  creadoEn: string;
}

@Injectable({ providedIn: 'root' })
export class FavoritoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly base = `${this.apiUrl}/api/favoritos`;

  getMisFavoritos(): Observable<FavoritoDto[]> {
    return this.http.get<FavoritoDto[]>(this.base);
  }

  agregar(libroId: string): Observable<unknown> {
    return this.http.post(`${this.base}/${libroId}`, {});
  }

  quitar(libroId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${libroId}`);
  }
}
