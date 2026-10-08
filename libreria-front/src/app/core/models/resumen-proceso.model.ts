export interface ResumenProceso {
  totalCatalogo: number;
  actualizados: number;
  sinCambio: number;
  nuevos: number;
  revisar: number;
  isbnInvalidos: number;
  archivoId: string;
  cambios: DetalleCambio[];
}

export interface DetalleCambio {
  isbn: string;
  titulo: string;
  proveedor: string;
  precioAnterior: number;
  precioNuevo: number;
  resultado: 'Actualizado' | 'Nuevo' | 'SinCambio' | 'Revisar';
  detalle: string;
}
