export interface LibroDto {
  id: string;
  isbn: string;
  titulo: string;
  autor: string;
  editorial: string;
  precioVenta: number;
  costo: number;
  descuento: number;
  portada?: string;
  sinopsis?: string;
  genero?: string;
  paginas?: number;
  anioPublicacion?: number;
  codigoBarra?: string;
  isActive: boolean;
  creadoEn: string;
  existencia?: number;
  ventas?: number;
  estadoInventario?: string;
  tipoUbicacion?: string;
  seccion?: string;
  estante?: string;
  referencia?: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CrearLibroRequest {
  isbn: string;
  titulo: string;
  autor: string;
  editorial: string;
  precioVenta: number;
  costo: number;
  descuento?: number;
  portada?: string;
  sinopsis?: string;
  genero?: string;
  paginas?: number;
  anioPublicacion?: number;
  codigoBarra?: string;
}

export interface ActualizarPrecioRequest {
  precioVenta: number;
  costo?: number;
  fuente?: string;
}

export interface ActualizarInventarioRequest {
  existencia: number;
}
