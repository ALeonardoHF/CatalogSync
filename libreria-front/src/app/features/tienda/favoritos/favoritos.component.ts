import { Component, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe } from '@angular/common';
import { FavoritoService, FavoritoDto } from '../../../core/services/favorito.service';
import { NotificacionService } from '../../../core/services/notificacion.service';

@Component({
  selector: 'app-favoritos',
  standalone: true,
  imports: [DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe],
  template: `
    <div class="page">
      <h2>Mis Favoritos</h2>

      @if (msj()) {
        <div class="toast" [ngClass]="msjTipo()">{{ msj() }}</div>
      }

      @if (loading()) {
        <p class="loading">Cargando...</p>
      } @else if (favoritos().length === 0) {
        <div class="empty">
          <div class="empty-icon">♡</div>
          <p>No tienes libros guardados.</p>
          <p class="empty-hint">Explora el catálogo y presiona el corazón para guardar libros.</p>
        </div>
      } @else {
        <div class="lista">
          @for (f of favoritos(); track f.id) {
            <div class="fav-card">
              @if (f.portada) {
                <img [src]="f.portada" [alt]="f.titulo" class="portada" />
              } @else {
                <div class="portada-placeholder">
                  <svg width="28" height="28" viewBox="0 0 24 24" fill="none">
                    <path d="M4 19.5A2.5 2.5 0 016.5 17H20" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
                    <path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" stroke="currentColor" stroke-width="1.5"/>
                  </svg>
                </div>
              }
              <div class="info">
                <div class="titulo">{{ f.titulo | lowercase | titlecase }}</div>
                <div class="autor">{{ f.autor | lowercase | titlecase }} · {{ f.editorial | lowercase | titlecase }}</div>
                <div class="isbn">{{ f.isbn }}</div>
                <div class="precio">\${{ f.precioVenta | number:'1.2-2' }}</div>
                <div [ngClass]="estadoClass(f)">
                  {{ f.estadoInventario ?? '—' }}
                  @if ((f.existencia ?? 0) > 0) { &nbsp;({{ f.existencia }} en stock) }
                </div>
              </div>
              <div class="acciones">
                @if ((f.existencia ?? 0) === 0) {
                  <button class="btn-notif" (click)="solicitar(f)">Avisarme</button>
                }
                <button class="btn-quitar" (click)="quitar(f)">Quitar</button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    h2 { margin: 0 0 1.25rem; font-size: 1.3rem; color: var(--text); }

    .toast { padding: .55rem .9rem; border-radius: 6px; margin-bottom: 1rem; font-size: .875rem; }
    .toast.success { background: #4ade8015; color: var(--success); border: 1px solid #4ade8030; }
    .toast.error   { background: #f8717115; color: var(--error);   border: 1px solid #f8717130; }

    .loading { text-align: center; color: var(--muted); padding: 2.5rem; }

    .empty { text-align: center; padding: 3.5rem 1rem; color: var(--muted); }
    .empty-icon { font-size: 2.5rem; margin-bottom: .75rem; opacity: .3; }
    .empty p { margin: .25rem 0; }
    .empty-hint { font-size: .875rem; opacity: .7; }

    .lista { display: flex; flex-direction: column; gap: .65rem; }

    .fav-card {
      display: flex; align-items: center; gap: 1rem;
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 10px; padding: .85rem 1rem;
      transition: border-color .15s;
    }
    .fav-card:hover { border-color: var(--accent); }

    .portada {
      width: 56px; height: 80px; object-fit: cover;
      border-radius: 5px; flex-shrink: 0;
      box-shadow: 0 2px 6px rgba(0,0,0,.4);
    }
    .portada-placeholder {
      width: 56px; height: 80px; flex-shrink: 0; border-radius: 5px;
      background: linear-gradient(135deg, #1e1e38, #2a2a48);
      border: 1px solid var(--border);
      display: flex; align-items: center; justify-content: center;
      color: var(--muted);
    }

    .info { flex: 1; min-width: 0; }
    .titulo { font-weight: 600; color: var(--text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .autor  { font-size: .83rem; color: var(--muted); margin-top: .1rem; }
    .isbn   { font-size: .75rem; color: var(--muted); font-family: monospace; margin-top: .1rem; opacity: .6; }
    .precio { font-size: 1.05rem; font-weight: 700; color: var(--success); margin-top: .35rem; }

    .estado-badge { font-size: .75rem; padding: .18rem .5rem; border-radius: 20px; display: inline-block; margin-top: .2rem; font-weight: 500; }
    .badge-disponible { background: #4ade8018; color: var(--success); border: 1px solid #4ade8030; }
    .badge-agotado    { background: #f8717118; color: var(--error);   border: 1px solid #f8717130; }
    .badge-default    { background: #6b6b8a18; color: var(--muted);   border: 1px solid var(--border); }

    .acciones { display: flex; flex-direction: column; gap: .45rem; align-items: flex-end; flex-shrink: 0; }
    .btn-notif {
      all: unset; cursor: pointer;
      font-size: .8rem; padding: .35rem .8rem;
      border: 1px solid #5865f260; color: #818cf8;
      border-radius: 6px; white-space: nowrap;
      transition: border-color .15s, background .15s;
    }
    .btn-notif:hover { border-color: #818cf8; background: #818cf810; }
    .btn-quitar {
      all: unset; cursor: pointer;
      font-size: .8rem; padding: .35rem .8rem;
      border: 1px solid #f8717150; color: var(--error);
      border-radius: 6px; white-space: nowrap;
      transition: border-color .15s, background .15s;
    }
    .btn-quitar:hover { border-color: var(--error); background: #f8717110; }
  `]
})
export class FavoritosComponent implements OnInit {
  private readonly favSvc   = inject(FavoritoService);
  private readonly notifSvc = inject(NotificacionService);

  readonly favoritos = signal<FavoritoDto[]>([]);
  readonly loading   = signal(false);
  readonly msj       = signal('');
  readonly msjTipo   = signal<'success' | 'error'>('success');

  ngOnInit(): void {
    this.loading.set(true);
    this.favSvc.getMisFavoritos().subscribe({
      next: f => { this.favoritos.set(f); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  quitar(fav: FavoritoDto): void {
    this.favSvc.quitar(fav.libroId).subscribe({
      next: () => {
        this.favoritos.update(prev => prev.filter(f => f.id !== fav.id));
        this.mostrarMsj('Quitado de favoritos', 'success');
      },
      error: () => this.mostrarMsj('Error al quitar de favoritos', 'error')
    });
  }

  solicitar(fav: FavoritoDto): void {
    this.notifSvc.solicitar(fav.libroId).subscribe({
      next: () => this.mostrarMsj(`Te avisaremos cuando "${fav.titulo}" esté disponible.`, 'success'),
      error: err => this.mostrarMsj(err.error?.message ?? 'Error al solicitar notificación', 'error')
    });
  }

  estadoClass(fav: FavoritoDto): string {
    const e = (fav.estadoInventario ?? '').toLowerCase();
    if (e === 'disponible') return 'estado-badge badge-disponible';
    if (e === 'agotado')    return 'estado-badge badge-agotado';
    return 'estado-badge badge-default';
  }

  private mostrarMsj(msg: string, tipo: 'success' | 'error'): void {
    this.msj.set(msg);
    this.msjTipo.set(tipo);
    setTimeout(() => this.msj.set(''), 3500);
  }
}
