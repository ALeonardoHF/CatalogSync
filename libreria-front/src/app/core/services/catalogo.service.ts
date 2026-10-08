import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { ResumenProceso } from '../models/resumen-proceso.model';

export interface EntradaItem {
  file: File;
  hoja?: string;
}

export type EstrategiaPrecio = 
'SiempreElMasAlto' | 
'SiempreElNuevo' |
'MasAltoSiHayExistencia';

@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  procesar(
    existencias: File,
    existenciasHoja: string | undefined,
    entradas: EntradaItem[],
    estrategia: EstrategiaPrecio
  ): Observable<ResumenProceso> {
    const form = new FormData();
    form.append('existencias', existencias);
    if (existenciasHoja?.trim()) form.append('existenciasHoja', existenciasHoja.trim());
    form.append('estrategia', estrategia);

    for (const e of entradas) {
      form.append('entradas', e.file);
      form.append('entradasHoja', e.hoja?.trim() ?? '');
    }

    return this.http.post<ResumenProceso>(`${this.apiUrl}/api/catalogo/procesar`, form);
  }

  importarBd(
    existencias: File,
    existenciasHoja: string | undefined,
    entradas: EntradaItem[],
    estrategia: EstrategiaPrecio
  ): Observable<ImportarCatalogoResult> {
    const form = new FormData();
    form.append('existencias', existencias);
    if (existenciasHoja?.trim()) form.append('existenciasHoja', existenciasHoja.trim());
    form.append('estrategia', estrategia);
    
    for (const e of entradas) {
      form.append('entradas', e.file);
      form.append('entradasHoja', e.hoja?.trim() ?? '');
    }

    return this.http.post<ImportarCatalogoResult>(`${this.apiUrl}/api/catalogo/importar-bd`, form);
  }

  importarArchivo(
    archivo: File,
    hoja?: string,
    proveedor?: string
  ): Observable<ImportarCatalogoResult> {
    const form = new FormData();
    form.append('archivo', archivo);
    if (hoja?.trim())      form.append('hoja', hoja.trim());
    if (proveedor?.trim()) form.append('proveedor', proveedor.trim());
    return this.http.post<ImportarCatalogoResult>(`${this.apiUrl}/api/catalogo/importar-archivo`, form);
  }

  descargarUrl(archivoId: string): string {
    return `${this.apiUrl}/api/catalogo/descargar/${archivoId}`;
  }
}

export interface ImportarCatalogoResult {
  creados: number;
  preciosActualizados: number;
  sinCambio: number;
  errores: number;
  mensajesError: string[];
  revisar: number;
  mensajesRevisar: string[];
}
