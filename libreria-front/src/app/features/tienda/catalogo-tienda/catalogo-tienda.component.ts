import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe } from '@angular/common';
import { LibroService } from '../../../core/services/libro.service';
import { FavoritoService } from '../../../core/services/favorito.service';
import { NotificacionService } from '../../../core/services/notificacion.service';
import { LibroDto } from '../../../core/models/libro.models';

@Component({
  selector: 'app-catalogo-tienda',
  standalone: true,
  imports: [FormsModule, DecimalPipe, NgClass, LowerCasePipe, TitleCasePipe],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Catálogo</h2>
      </div>

      <!-- Barra de búsqueda + filtro -->
      <div class="search-row">
        <input type="search" placeholder="Buscar por ISBN, título, autor..."
               [(ngModel)]="termino" (keyup.enter)="buscar()" />
        <button class="btn-search" (click)="buscar()" [disabled]="loading()">
          {{ loading() ? 'Buscando...' : 'Buscar' }}
        </button>
      </div>

      <div class="filtros-row">
        <button class="chip" [class.active]="!soloConExistencia()" (click)="setFiltro(false)">
          Todos
        </button>
        <button class="chip" [class.active]="soloConExistencia()" (click)="setFiltro(true)">
          Solo con existencia
        </button>
      </div>

      @if (msj()) {
        <div class="toast" [ngClass]="msjTipo()">{{ msj() }}</div>
      }

      <div class="grid">
        @for (libro of libros(); track libro.id) {
          <div class="libro-card">
            <div class="portada-wrap">
              @if (libro.portada) {
                <img [src]="libro.portada" [alt]="libro.titulo" class="portada-img" />
              } @else {
                <div class="portada-placeholder">
                  <svg width="44" height="44" viewBox="0 0 24 24" fill="none" opacity="0.35">
                    <path d="M4 19.5A2.5 2.5 0 016.5 17H20" stroke="#fff" stroke-width="1.5" stroke-linecap="round"/>
                    <path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" stroke="#fff" stroke-width="1.5"/>
                  </svg>
                </div>
              }
            </div>

            <div class="libro-body">
              <p class="titulo">{{ libro.titulo | lowercase | titlecase }}</p>
              <p class="autor">{{ libro.autor | lowercase | titlecase }}</p>
              <p class="editorial">{{ libro.editorial | lowercase | titlecase }}</p>

              <div class="footer">
                <span class="precio">\${{ libro.precioVenta | number:'1.0-0' }}</span>
                <div [ngClass]="estadoClass(libro)">{{ estadoLabel(libro) }}</div>
              </div>

              <div class="acciones">
                <button class="btn-fav" (click)="toggleFavorito(libro)"
                        [class.fav-on]="esFavorito(libro.id)"
                        [title]="esFavorito(libro.id) ? 'Quitar de favoritos' : 'Agregar a favoritos'">
                  ♥
                </button>
                @if ((libro.existencia ?? 0) === 0) {
                  <button class="btn-notif" (click)="solicitar(libro)">Avisarme</button>
                }
              </div>
            </div>
          </div>
        } @empty {
          @if (buscado()) {
            <p class="empty">No se encontraron libros.</p>
          }
        }
      </div>

      @if (total() > libros().length) {
        <div class="ver-mas-row">
          <button class="btn-ver-mas" (click)="siguiente()" [disabled]="loading()">
            {{ loading() ? 'Cargando...' : 'Ver más (' + (total() - libros().length) + ' restantes)' }}
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-header { margin-bottom: 1rem; }
    h2 { margin: 0 0 .5rem; font-size: 1.4rem; }

    .search-row {
      display: flex; gap: .75rem; margin-bottom: .75rem;
    }
    .search-row input {
      flex: 1; padding: .65rem .85rem; border: 1px solid #ddd;
      border-radius: 6px; font-size: 1rem;
    }
    .search-row input:focus { outline: none; border-color: #5865f2; }

    .btn-search {
      padding: .65rem 1.5rem; background: #5865f2; color: white;
      border: none; border-radius: 6px; cursor: pointer; white-space: nowrap;
    }
    .btn-search:disabled { opacity: .6; cursor: not-allowed; }

    .filtros-row {
      display: flex; gap: .5rem; margin-bottom: 1.25rem;
    }
    .chip {
      padding: .35rem .9rem; border: 1.5px solid #ddd; border-radius: 20px;
      background: white; cursor: pointer; font-size: .85rem; color: #555;
      transition: all .15s;
    }
    .chip:hover { border-color: #5865f2; color: #5865f2; }
    .chip.active { border-color: #5865f2; background: #5865f2; color: white; }

    .toast { padding: .6rem 1rem; border-radius: 6px; margin-bottom: 1rem; font-size: .875rem; }
    .toast.success { background: #d4edda; color: #155724; }
    .toast.error   { background: #f8d7da; color: #721c24; }

    /* ── Grid ── */
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
      gap: 1.25rem;
      align-items: start;
    }

    /* ── Card ── */
    .libro-card {
      background: white;
      border-radius: 14px;
      box-shadow: 0 2px 8px rgba(0,0,0,.07);
      border: 1px solid #ebebeb;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      height: 100%;
      transition: box-shadow .2s, transform .2s;
    }
    .libro-card:hover {
      box-shadow: 0 6px 20px rgba(0,0,0,.12);
      transform: translateY(-2px);
    }

    /* ── Portada ── */
    .portada-wrap {
      aspect-ratio: 2/3;
      background: #f5f5f5;
      overflow: hidden;
      flex-shrink: 0;
    }
    .portada-img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .portada-placeholder {
      width: 100%; height: 100%;
      display: flex; align-items: center; justify-content: center;
      background: linear-gradient(160deg, #1a1a2e 0%, #2a2040 100%);
    }

    /* ── Cuerpo ── */
    .libro-body {
      padding: .9rem .85rem .75rem;
      display: flex; flex-direction: column; flex: 1;
    }

    .titulo {
      font-size: .88rem; font-weight: 700; color: #1a1a1a;
      line-height: 1.35; margin: 0 0 .2rem;
      display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .autor {
      font-size: .78rem; color: #555; margin: 0 0 .15rem;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .editorial {
      font-size: .72rem; color: #999; margin: 0 0 .65rem; letter-spacing: .02em;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }

    /* ── Footer dentro del body ── */
    .footer {
      display: flex; align-items: center; justify-content: space-between;
      gap: .4rem; margin-top: auto; flex-wrap: wrap;
    }
    .precio {
      font-size: 1.15rem; font-weight: 800; color: #1e7e4a; letter-spacing: -.01em;
    }

    .estado-badge {
      font-size: .7rem; padding: .18rem .55rem; border-radius: 20px;
      font-weight: 600; white-space: nowrap;
    }
    .estado-disponible  { background: #e6f4ec; color: #1e7e4a; }
    .estado-agotado     { background: #fce8e8; color: #b91c1c; }
    .estado-desconocido { background: #f0f0f0; color: #777; }

    /* ── Acciones ── */
    .acciones {
      display: flex; align-items: center; gap: .5rem; margin-top: .65rem;
    }
    .btn-fav {
      all: unset;
      cursor: pointer; font-size: 1.1rem; line-height: 1;
      color: #ddd; transition: color .15s, transform .15s;
      flex-shrink: 0;
    }
    .btn-fav:hover { color: #f87171; transform: scale(1.15); }
    .btn-fav.fav-on { color: #ef4444; }

    .btn-notif {
      flex: 1; font-size: .78rem; padding: .3rem .5rem;
      border: 1.5px solid #5865f2; color: #5865f2;
      border-radius: 6px; cursor: pointer; background: transparent;
      font-weight: 500; transition: background .15s;
    }
    .btn-notif:hover { background: #f0f1fe; }

    .empty { grid-column: 1/-1; text-align: center; color: #888; padding: 3rem; }

    .ver-mas-row { display: flex; justify-content: center; margin-top: 2.5rem; }
    .btn-ver-mas {
      padding: .75rem 2.5rem; background: #5865f2; color: white;
      border: none; border-radius: 8px; font-size: 1rem; font-weight: 600;
      cursor: pointer; transition: background .15s;
    }
    .btn-ver-mas:hover:not(:disabled) { background: #4752c4; }
    .btn-ver-mas:disabled { opacity: .6; cursor: not-allowed; }
  `]
})
export class CatalogoTiendaComponent implements OnInit {
  private readonly libroSvc = inject(LibroService);
  private readonly favSvc   = inject(FavoritoService);
  private readonly notifSvc = inject(NotificacionService);

  termino = '';
  page = 1;

  readonly libros          = signal<LibroDto[]>([]);
  readonly total           = signal(0);
  readonly loading         = signal(false);
  readonly buscado         = signal(false);
  readonly favoritosIds    = signal<Set<string>>(new Set());
  readonly msj             = signal('');
  readonly msjTipo         = signal<'success' | 'error'>('success');
  readonly soloConExistencia = signal(false);

  ngOnInit(): void {
    this.cargarFavoritos();
    this.buscar();
  }

  setFiltro(valor: boolean): void {
    if (this.soloConExistencia() === valor) return;
    this.soloConExistencia.set(valor);
    this.buscar();
  }

  buscar(): void {
    this.page = 1;
    this.libros.set([]);
    this.cargar();
  }

  siguiente(): void {
    this.page++;
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.libroSvc.buscar(this.termino || undefined, this.page, 20, this.soloConExistencia(), true).subscribe({
      next: r => {
        this.libros.update(prev => [...prev, ...r.items]);
        this.total.set(r.total);
        this.loading.set(false);
        this.buscado.set(true);
      },
      error: () => this.loading.set(false)
    });
  }

  cargarFavoritos(): void {
    this.favSvc.getMisFavoritos().subscribe({
      next: favs => this.favoritosIds.set(new Set(favs.map(f => f.libroId)))
    });
  }

  esFavorito(libroId: string): boolean {
    return this.favoritosIds().has(libroId);
  }

  toggleFavorito(libro: LibroDto): void {
    if (this.esFavorito(libro.id)) {
      this.favSvc.quitar(libro.id).subscribe({
        next: () => {
          this.favoritosIds.update(s => { const n = new Set(s); n.delete(libro.id); return n; });
          this.mostrarMsj('Quitado de favoritos', 'success');
        }
      });
    } else {
      this.favSvc.agregar(libro.id).subscribe({
        next: () => {
          this.favoritosIds.update(s => new Set([...s, libro.id]));
          this.mostrarMsj('Agregado a favoritos', 'success');
        },
        error: () => this.mostrarMsj('No se pudo agregar a favoritos', 'error')
      });
    }
  }

  solicitar(libro: LibroDto): void {
    this.notifSvc.solicitar(libro.id).subscribe({
      next: () => this.mostrarMsj(`Te avisaremos cuando "${libro.titulo}" esté disponible.`, 'success'),
      error: err => this.mostrarMsj(err.error?.message ?? 'Error al solicitar notificación', 'error')
    });
  }

  estadoLabel(libro: LibroDto): string {
    const e = (libro.estadoInventario ?? '').toLowerCase();
    if (e === 'disponible') {
      const cant = libro.existencia ?? 0;
      return cant > 0 ? `Disponible (${cant})` : 'Disponible';
    }
    if (e === 'agotado') return 'Agotado';
    return libro.estadoInventario ?? '';
  }

  estadoClass(libro: LibroDto): string {
    const e = (libro.estadoInventario ?? '').toLowerCase();
    if (e === 'disponible') return 'estado-badge estado-disponible';
    if (e === 'agotado')    return 'estado-badge estado-agotado';
    return 'estado-badge estado-desconocido';
  }

  private mostrarMsj(msg: string, tipo: 'success' | 'error'): void {
    this.msj.set(msg);
    this.msjTipo.set(tipo);
    setTimeout(() => this.msj.set(''), 3000);
  }
}
