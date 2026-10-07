import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import {
  ActualizarInventarioRequest,
  ActualizarPrecioRequest,
  CrearLibroRequest,
  LibroDto,
  PagedResult
} from '../models/libro.models';

@Injectable({ providedIn: 'root' })
export class LibroService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly base = `${this.apiUrl}/api/libros`;

  buscar(q?: string, page = 1, pageSize = 20, soloConExistencia = false, isActive: boolean | null = true, sinExistencia = false): Observable<PagedResult<LibroDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (q?.trim())         params = params.set('q', q.trim());
    if (soloConExistencia) params = params.set('soloConExistencia', 'true');
    if (sinExistencia)     params = params.set('sinExistencia', 'true');
    if (isActive !== null) params = params.set('isActive', String(isActive));
    return this.http.get<PagedResult<LibroDto>>(this.base, { params });
  }

  getById(id: string): Observable<LibroDto> {
    return this.http.get<LibroDto>(`${this.base}/${id}`);
  }

  crear(request: CrearLibroRequest): Observable<LibroDto> {
    return this.http.post<LibroDto>(this.base, request);
  }

  actualizar(id: string, request: Partial<CrearLibroRequest>): Observable<LibroDto> {
    return this.http.put<LibroDto>(`${this.base}/${id}`, request);
  }

  actualizarPrecio(id: string, request: ActualizarPrecioRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/precio`, request);
  }

  actualizarInventario(id: string, request: ActualizarInventarioRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/inventario`, request);
  }

  desactivar(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/desactivar`, {});
  }

  activar(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/activar`, {});
  }

  bulkAccion(ids: string[], accion: 'activar' | 'desactivar'): Observable<{ afectados: number; errores: number }> {
    return this.http.post<{ afectados: number; errores: number }>(`${this.base}/bulk/accion`, { ids, accion });
  }

  desactivarAgotados(): Observable<{ afectados: number; errores: number }> {
    return this.http.post<{ afectados: number; errores: number }>(`${this.base}/bulk/desactivar-agotados`, {});
  }

  subirPortada(id: string, archivo: File): Observable<{ url: string }> {
    const fd = new FormData();
    fd.append('archivo', archivo);
    return this.http.post<{ url: string }>(`${this.base}/${id}/portada`, fd);
  }

  subirPortadasBulk(archivos: File[]): Observable<BulkPortadasResult> {
    const fd = new FormData();
    archivos.forEach(f => fd.append('archivos', f));
    return this.http.post<BulkPortadasResult>(`${this.base}/portadas/bulk`, fd);
  }
}

export interface BulkPortadasItem {
  archivo: string;
  isbn: string;
  titulo: string | null;
  resultado: 'OK' | 'NoEncontrado' | 'ExtensionNoValida' | 'DemasiadoGrande';
}

export interface BulkPortadasResult {
  asignadas: number;
  noEncontradas: number;
  errores: number;
  detalles: BulkPortadasItem[];
}
