import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe } from '@angular/common';
import { LibroService } from '../../../core/services/libro.service';
import { LibroDto } from '../../../core/models/libro.models';

@Component({
  selector: 'app-busqueda',
  standalone: true,
  imports: [FormsModule, DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe],
  template: `
    <div class="page">
      <h2>Búsqueda de Inventario</h2>

      <div class="search-row">
        <input type="search" placeholder="ISBN, título, autor, editorial..."
               [(ngModel)]="termino" (keyup.enter)="buscar()" />
        <button class="btn-buscar" (click)="buscar()" [disabled]="loading()">
          {{ loading() ? 'Buscando...' : 'Buscar' }}
        </button>
      </div>

      @if (buscado() && libros().length === 0) {
        <p class="empty">No se encontraron libros.</p>
      }

      @if (libros().length > 0) {
        <div class="resultados">
          @for (libro of libros(); track libro.id) {
            <div class="libro-card" [ngClass]="{ agotado: (libro.existencia ?? 0) === 0 }">
              <div class="isbn">{{ libro.isbn }}</div>
              <div class="libro-info">
                <div class="titulo">{{ libro.titulo | lowercase | titlecase }}</div>
                <div class="meta">
                  {{ libro.autor | lowercase | titlecase }}
                  &nbsp;·&nbsp;
                  {{ libro.editorial | lowercase | titlecase }}
                </div>
              </div>
              <div class="libro-stock">
                <div class="precio">\${{ libro.precioVenta | number:'1.2-2' }}</div>
                <div [ngClass]="stockClass(libro)">{{ libro.existencia ?? 0 }} en stock</div>
                @if (libro.seccion || libro.estante) {
                  <div class="ubicacion">
                    {{ libro.tipoUbicacion }} · {{ libro.seccion }} {{ libro.estante }}
                  </div>
                }
              </div>
            </div>
          }
        </div>

        @if (total() > libros().length) {
          <p class="more">Mostrando {{ libros().length }} de {{ total() }} resultados. Refina la búsqueda.</p>
        }
      }
    </div>
  `,
  styles: [`
    .page { }
    h2 { margin: 0 0 1.25rem; font-size: 1.3rem; color: var(--text); }

    .search-row { display: flex; gap: .75rem; margin-bottom: 1.5rem; }
    .search-row input {
      flex: 1; padding: .65rem .85rem;
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 8px; color: var(--text); font-size: 1rem;
    }
    .search-row input:focus { outline: none; border-color: var(--primary); }
    .btn-buscar {
      all: unset; cursor: pointer;
      padding: .65rem 1.75rem; background: var(--primary); color: #fff;
      border-radius: 8px; font-size: .95rem; font-weight: 500; white-space: nowrap;
      transition: opacity .15s;
    }
    .btn-buscar:hover:not(:disabled) { opacity: .85; }
    .btn-buscar:disabled { opacity: .5; cursor: not-allowed; }

    .empty { color: var(--muted); text-align: center; padding: 2rem; }
    .resultados { display: flex; flex-direction: column; gap: .6rem; }

    .libro-card {
      display: flex; align-items: center; gap: 1rem;
      padding: .85rem 1rem;
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 8px; border-left: 3px solid var(--primary);
      transition: border-color .15s;
    }
    .libro-card:hover { border-color: var(--accent); }
    .libro-card.agotado { border-left-color: var(--border); opacity: .55; }

    .isbn { font-size: .78rem; color: var(--muted); min-width: 110px; font-family: monospace; }
    .libro-info { flex: 1; min-width: 0; }
    .titulo { font-weight: 600; color: var(--text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .meta { font-size: .82rem; color: var(--muted); margin-top: .1rem; }

    .libro-stock { text-align: right; min-width: 130px; }
    .precio { font-size: 1.05rem; font-weight: 700; color: var(--success); }

    .stock-badge { font-size: .8rem; margin-top: .15rem; padding: .2rem .5rem; border-radius: 12px; display: inline-block; }
    .stock-ok      { background: #4ade8018; color: var(--success); border: 1px solid #4ade8030; }
    .stock-bajo    { background: #fbbf2418; color: #fbbf24;        border: 1px solid #fbbf2430; }
    .stock-agotado { background: #f8717118; color: var(--error);   border: 1px solid #f8717130; }

    .ubicacion { font-size: .75rem; color: var(--muted); margin-top: .2rem; }
    .more { color: var(--muted); text-align: center; margin-top: 1rem; font-size: .88rem; }
  `]
})
export class BusquedaComponent {
  private readonly svc = inject(LibroService);

  termino = '';
  readonly libros  = signal<LibroDto[]>([]);
  readonly total   = signal(0);
  readonly loading = signal(false);
  readonly buscado = signal(false);

  buscar(): void {
    if (!this.termino.trim()) return;
    this.loading.set(true);

    this.svc.buscar(this.termino, 1, 50, false, true).subscribe({
      next: r => {
        this.libros.set(r.items);
        this.total.set(r.total);
        this.loading.set(false);
        this.buscado.set(true);
      },
      error: () => this.loading.set(false)
    });
  }

  stockClass(libro: LibroDto): string {
    const e = libro.existencia ?? 0;
    if (e === 0) return 'stock-badge stock-agotado';
    if (e <= 3)  return 'stock-badge stock-bajo';
    return 'stock-badge stock-ok';
  }
}
