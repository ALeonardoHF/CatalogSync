import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CatalogoService, EntradaItem, ImportarCatalogoResult, EstrategiaPrecio } from '../../core/services/catalogo.service';
import { NotificationService } from '../../core/services/notification.service';
import { LibroService } from '../../core/services/libro.service';
import { LoadingButtonDirective } from '../../shared/directives/loading-button.directive';
import { ResumenProceso } from '../../core/models/resumen-proceso.model';

interface EntradaRow {
  id: number;
  file?: File;
  hoja: string;
}

type Vista = 'comparar' | 'importar';

@Component({
  selector: 'app-catalogo',
  standalone: true,
  imports: [CommonModule, AsyncPipe, FormsModule, LoadingButtonDirective],
  template: `
    <div class="container wide">

      <!-- Tabs -->
      <div class="tabs">
        <button class="tab" [class.active]="vista() === 'importar'" (click)="vista.set('importar')">
          {{ dbTieneLibros() ? 'Actualizar precios / stock' : 'Carga inicial' }}
        </button>
        <button class="tab" [class.active]="vista() === 'comparar'" (click)="vista.set('comparar')">
          Comparar catálogos
        </button>
      </div>

      @if (notif.current$ | async; as n) {
        <div class="notification" [class]="n.type">{{ n.message }}</div>
      }

      <!-- ══════════ VISTA: Importar / Actualizar ══════════ -->
      @if (vista() === 'importar') {
        <div class="import-panel">

          @if (!dbTieneLibros()) {
            <div class="banner banner-warn">
              <strong>Base de datos vacía</strong> — Esta importación creará todos los libros desde cero.
              Asegúrate de que el archivo tenga al menos las columnas <code>ISBN</code> y <code>PRECIO</code>.
            </div>
          } @else {
            <div class="banner banner-info">
              Sube el archivo del proveedor y se actualizarán <strong>precios y existencias</strong> de los libros
              existentes. Los ISBNs nuevos se agregarán automáticamente.
            </div>
          }

          <form (ngSubmit)="importarArchivo()">
            <div class="card" style="display:flex; flex-direction:column; gap:12px">
              <strong>{{ dbTieneLibros() ? 'Archivo del proveedor' : 'Archivo a importar' }}</strong>
              <div style="display:grid; grid-template-columns:1fr 180px 200px; gap:10px; align-items:end">
                <div class="field">
                  <label>Archivo <span style="color:var(--error)">*</span></label>
                  <input type="file" accept=".xls,.xlsx,.xlsm,.xlsb,.csv"
                         (change)="onArchivoFile($event)" />
                </div>
                <div class="field">
                  <label>Hoja (opcional)</label>
                  <input type="text" placeholder="nombre o índice"
                         [(ngModel)]="archivoHoja" name="archivoHoja" />
                </div>
                <div class="field">
                  <label>Nombre proveedor (opcional)</label>
                  <input type="text" placeholder="ej. DISTRIBUIDORA"
                         [(ngModel)]="archivoProveedor" name="archivoProveedor" />
                </div>
              </div>
              <p class="hint">
                Se detectan automáticamente: <code>ISBN</code>, <code>PRECIO / COSTO</code>,
                <code>TITULO / NOMBRE</code>, <code>AUTOR</code>, <code>EDITORIAL</code>,
                <code>EXISTENCIA</code>, <code>CODIGOBARRA</code>
              </p>
            </div>

            <button type="submit" [appLoadingButton]="cargandoArchivo()"
                    [disabled]="!archivoFile || cargandoArchivo()">
              {{ cargandoArchivo() ? 'Importando...' : (dbTieneLibros() ? 'Actualizar base de datos' : 'Importar a base de datos') }}
            </button>
          </form>

          @if (archivoResult()) {
            <div class="import-result">
              <strong>Resultado:</strong>
              <span class="badge new">{{ archivoResult()!.creados }} creados</span>
              <span class="badge updated">{{ archivoResult()!.preciosActualizados }} actualizados</span>
              <span class="badge">{{ archivoResult()!.sinCambio }} sin cambio</span>
              @if (archivoResult()!.errores > 0) {
                <span class="badge invalid">{{ archivoResult()!.errores }} errores</span>
              }
            </div>
            @if (archivoResult()!.mensajesError.length > 0) {
              <ul class="error-list">
                @for (e of archivoResult()!.mensajesError; track $index) {
                  <li>{{ e }}</li>
                }
              </ul>
            }
          }
        </div>
      }

      <!-- ══════════ VISTA: Comparar catálogos ══════════ -->
      @if (vista() === 'comparar') {

        <div class="banner banner-info" style="margin-bottom:1rem">
          Sube el archivo de <strong>existencias actuales</strong> más los archivos de
          <strong>entradas</strong> (nuevas llegadas). El sistema genera un Excel comparado
          que puedes revisar antes de importar a la base de datos.
        </div>

        <form (ngSubmit)="procesar()">

          <!-- Existencias -->
          <div class="card" style="display:flex; flex-direction:column; gap:12px">
            <strong>Archivo de existencias actuales</strong>
            <div style="display:grid; grid-template-columns:1fr 180px; gap:10px; align-items:end">
              <div class="field">
                <label>Archivo <span style="color:var(--error)">*</span></label>
                <input type="file" accept=".xls,.xlsx,.xlsm,.xlsb,.csv"
                       (change)="onExistenciasFile($event)" />
              </div>
              <div class="field">
                <label>Hoja (opcional)</label>
                <input type="text" placeholder="nombre o índice"
                       [(ngModel)]="existenciasHoja" name="existenciasHoja" />
              </div>
            </div>
          </div>

          <!-- Entradas dinámicas -->
          <div class="card" style="display:flex; flex-direction:column; gap:12px">
            <div style="display:flex; justify-content:space-between; align-items:center">
              <strong>Archivos de entrada (nuevas llegadas)</strong>
              <button type="button" class="secondary" style="width:auto"
                      (click)="agregarEntrada()">
                + Agregar archivo
              </button>
            </div>

            @for (entrada of entradas; track entrada.id) {
              <div style="display:grid; grid-template-columns:1fr 180px 36px; gap:10px; align-items:end">
                <div class="field">
                  <label>Archivo {{ entradas.length > 1 ? ($index + 1) : '' }}</label>
                  <input type="file" accept=".xls,.xlsx,.xlsm,.xlsb,.csv"
                         (change)="onEntradaFile($event, entrada.id)" />
                </div>
                <div class="field">
                  <label>Hoja (opcional)</label>
                  <input type="text" placeholder="nombre o índice"
                         [(ngModel)]="entrada.hoja"
                         [name]="'entradaHoja_' + entrada.id" />
                </div>
                @if (entradas.length > 1) {
                  <button type="button" class="secondary danger-text"
                          style="width:auto; padding:10px 8px; margin-bottom:0; align-self:end"
                          (click)="quitarEntrada(entrada.id)" title="Quitar">✕</button>
                } @else {
                  <div></div>
                }
              </div>
            }
          </div>

          <!-- Regla de precio -->
          <div class="card" style="display:flex; flex-direction:column; gap:12px">
            <strong>Regla de precio</strong>
            <div class="field">
              <select [(ngModel)]="estrategia" name="estrategia">
                <option value="MasAltoSiHayExistencia">Conservar el más alto si hay existencia</option>
                <option value="SiempreElMasAlto">Siempre el precio más alto</option>
                <option value="SiempreElNuevo">Siempre el precio nuevo</option>
              </select>
            </div>
          </div>


          <button type="submit" [appLoadingButton]="cargando()" [disabled]="!puedeEnviar()">
            {{ cargando() ? 'Procesando...' : 'Generar comparación' }}
          </button>
        </form>

        <!-- Resultado -->
        @if (resumen()) {
          <div class="stats-grid">
            <div class="stat-card">
              <div class="stat-value">{{ resumen()!.totalCatalogo | number }}</div>
              <div class="stat-label">Total catálogo</div>
            </div>
            <div class="stat-card updated">
              <div class="stat-value">{{ resumen()!.actualizados | number }}</div>
              <div class="stat-label">Actualizados</div>
            </div>
            <div class="stat-card new">
              <div class="stat-value">{{ resumen()!.nuevos | number }}</div>
              <div class="stat-label">Nuevos</div>
            </div>
            <div class="stat-card">
              <div class="stat-value">{{ resumen()!.sinCambio | number }}</div>
              <div class="stat-label">Sin cambio</div>
            </div>
            <div class="stat-card invalid">
              <div class="stat-value">{{ resumen()!.isbnInvalidos | number }}</div>
              <div class="stat-label">ISBNs inválidos</div>
            </div>
          </div>

          <div class="actions">
            <a class="btn download" style="width:auto"
               [href]="catalogoService.descargarUrl(resumen()!.archivoId)"
               download="existencias_actualizado.xlsx">
              Descargar existencias_actualizado.xlsx
            </a>
            <button type="button" class="btn import-bd" style="width:auto"
                    [disabled]="importando()"
                    (click)="importarBd()">
              {{ importando() ? 'Importando...' : 'Importar a base de datos' }}
            </button>
          </div>

          @if (importResult()) {
            <div class="import-result">
              <strong>Resultado de importación:</strong>
              <span class="badge new">{{ importResult()!.creados }} creados</span>
              <span class="badge updated">{{ importResult()!.preciosActualizados }} actualizados</span>
              <span class="badge">{{ importResult()!.sinCambio }} sin cambio</span>
              @if (importResult()!.errores > 0) {
                <span class="badge invalid">{{ importResult()!.errores }} errores</span>
              }
            </div>
          }

          <!-- Filtros -->
          <div class="filters card">
            <input type="text" placeholder="Buscar ISBN o título..."
                   [(ngModel)]="filtroTexto" name="filtroTexto" />
            <select [(ngModel)]="filtroResultado" name="filtroResultado">
              <option value="">Todos</option>
              <option value="Actualizado">Actualizados</option>
              <option value="Nuevo">Nuevos</option>
              <option value="SinCambio">Sin cambio</option>
            </select>
            @if (proveedoresDisponibles().length > 1) {
              <select [(ngModel)]="filtroProveedor" name="filtroProveedor">
                <option value="">Todos los proveedores</option>
                @for (p of proveedoresDisponibles(); track p) {
                  <option [value]="p">{{ p }}</option>
                }
              </select>
            }
          </div>

          <!-- Tabla -->
          <div class="table-card">
            <table>
              <thead>
                <tr>
                  <th>ISBN</th>
                  <th>Título</th>
                  <th>Proveedor</th>
                  <th>Precio anterior</th>
                  <th>Precio nuevo</th>
                  <th>Resultado</th>
                </tr>
              </thead>
              <tbody>
                @for (c of cambiosFiltrados(); track c.isbn + c.proveedor) {
                  <tr>
                    <td>{{ c.isbn }}</td>
                    <td>{{ c.titulo }}</td>
                    <td>{{ c.proveedor }}</td>
                    <td>{{ c.precioAnterior > 0 ? (c.precioAnterior | number:'1.2-2') : '—' }}</td>
                    <td>{{ c.precioNuevo | number:'1.2-2' }}</td>
                    <td>
                      <span class="badge" [class]="c.resultado.toLowerCase()">
                        {{ c.resultado === 'SinCambio' ? 'Sin cambio' : c.resultado }}
                      </span>
                    </td>
                  </tr>
                }
                @if (cambiosFiltrados().length === 0) {
                  <tr>
                    <td colspan="6" class="text-center text-muted" style="padding:20px">
                      Sin resultados
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <p class="text-muted text-center">
            Mostrando {{ cambiosFiltrados().length }} de {{ resumen()!.cambios.length }} registros
          </p>
        }
      }
    </div>
  `,
  styles: [`
    .tabs {
      display: flex; gap: 0; margin-bottom: 1.5rem;
      border-bottom: 2px solid var(--border);
    }
    .tab {
      all: unset; cursor: pointer;
      padding: .65rem 1.4rem; border-bottom: 2px solid transparent;
      margin-bottom: -2px; font-size: .9rem; color: var(--muted); transition: all .15s;
    }
    .tab:hover { color: var(--text); }
    .tab.active { color: var(--accent); border-bottom-color: var(--accent); font-weight: 600; }

    .import-panel { display: flex; flex-direction: column; gap: 1.25rem; }

    .banner {
      padding: .8rem 1rem; border-radius: 8px; font-size: .875rem; line-height: 1.55;
    }
    .banner code {
      padding: .1rem .4rem; border-radius: 4px; font-family: monospace; font-size: .82rem;
    }
    .banner-warn {
      background: #2a1f00; color: #fcd34d; border: 1px solid #fbbf2435;
    }
    .banner-warn strong { color: #fde68a; }
    .banner-warn code  { background: #3a2d00; color: #fde68a; }

    .banner-info {
      background: #0d1b2e; color: #93c5fd; border: 1px solid #3b82f635;
    }
    .banner-info strong { color: #bfdbfe; }
    .banner-info code  { background: #1e3a5f; color: #bfdbfe; }

    .hint { margin: .25rem 0 0; color: var(--muted); font-size: .82rem; line-height: 1.5; }
    .hint code {
      background: #1e1e38; color: var(--accent);
      padding: .1rem .35rem; border-radius: 4px; font-family: monospace; font-size: .8rem;
    }

    /* Estilizar el botón "Choose File" del input file */
    input[type="file"]::file-selector-button {
      background: var(--surface); color: var(--text);
      border: 1px solid var(--border); border-radius: 5px;
      padding: .3rem .75rem; margin-right: .75rem;
      cursor: pointer; font-size: .83rem; transition: border-color .15s;
    }
    input[type="file"]::file-selector-button:hover {
      border-color: var(--accent); color: var(--accent);
    }
    input[type="file"] { color: var(--muted); font-size: .85rem; }

    .import-result {
      display: flex; align-items: center; gap: .5rem; flex-wrap: wrap;
      padding: .75rem 1rem;
      background: #0d1b2e; border-radius: 8px; border: 1px solid #3b82f625;
      color: var(--text);
    }
    .error-list { margin: 0; padding-left: 1.25rem; font-size: .85rem; color: var(--error); }
    .error-list li { margin-bottom: .2rem; }
  `]
})
export class CatalogoComponent implements OnInit {
  readonly catalogoService = inject(CatalogoService);
  readonly notif           = inject(NotificationService);
  private readonly libroSvc = inject(LibroService);

  vista = signal<Vista>('importar');
  dbTieneLibros = signal(false);

  // Importar / Actualizar rápido
  archivoFile?: File;
  archivoHoja = '';
  archivoProveedor = '';
  cargandoArchivo = signal(false);
  archivoResult = signal<ImportarCatalogoResult | null>(null);

  // Comparar catálogos — existencias
  existenciasFile?: File;
  existenciasHoja = '';
  estrategia: EstrategiaPrecio = 'MasAltoSiHayExistencia';

  // Comparar catálogos — entradas dinámicas
  entradas: EntradaRow[] = [{ id: 1, hoja: '' }];
  private nextId = 2;

  // Estado comparar
  cargando  = signal(false);
  importando = signal(false);
  resumen   = signal<ResumenProceso | null>(null);
  importResult = signal<ImportarCatalogoResult | null>(null);

  // Filtros tabla comparación
  filtroTexto = '';
  filtroResultado = '';
  filtroProveedor = '';

  ngOnInit(): void {
    this.libroSvc.buscar(undefined, 1, 1, false, null).subscribe({
      next: r => this.dbTieneLibros.set(r.total > 0)
    });
  }

  puedeEnviar = computed(() => !!this.existenciasFile && !this.cargando());

  proveedoresDisponibles = computed(() => {
    const r = this.resumen();
    if (!r) return [];
    return [...new Set(r.cambios.map(c => c.proveedor))].sort();
  });

  cambiosFiltrados = computed(() => {
    const r = this.resumen();
    if (!r) return [];
    return r.cambios.filter(c => {
      const txt = this.filtroTexto.toLowerCase();
      return (!txt || c.isbn.toLowerCase().includes(txt) || c.titulo.toLowerCase().includes(txt))
          && (!this.filtroResultado || c.resultado === this.filtroResultado)
          && (!this.filtroProveedor || c.proveedor === this.filtroProveedor);
    });
  });

  onArchivoFile(e: Event): void {
    this.archivoFile = (e.target as HTMLInputElement).files?.[0];
    this.archivoResult.set(null);
  }

  onExistenciasFile(e: Event): void {
    this.existenciasFile = (e.target as HTMLInputElement).files?.[0];
  }

  onEntradaFile(e: Event, id: number): void {
    const entrada = this.entradas.find(x => x.id === id);
    if (entrada) entrada.file = (e.target as HTMLInputElement).files?.[0];
  }

  agregarEntrada(): void { this.entradas.push({ id: this.nextId++, hoja: '' }); }
  quitarEntrada(id: number): void { this.entradas = this.entradas.filter(e => e.id !== id); }

  importarArchivo(): void {
    if (!this.archivoFile || this.cargandoArchivo()) return;
    this.cargandoArchivo.set(true);
    this.archivoResult.set(null);

    this.catalogoService
      .importarArchivo(this.archivoFile, this.archivoHoja || undefined, this.archivoProveedor || undefined)
      .subscribe({
        next: r => {
          this.archivoResult.set(r);
          this.dbTieneLibros.set(true);
          this.notif.show(`Completado: ${r.creados} creados, ${r.preciosActualizados} actualizados.`, 'success');
          this.cargandoArchivo.set(false);
        },
        error: err => {
          this.notif.show(err.error?.message ?? err.error ?? 'Error al importar.', 'error');
          this.cargandoArchivo.set(false);
        }
      });
  }

  importarBd(): void {
    if (!this.existenciasFile || this.importando()) return;
    const entradasValidas: EntradaItem[] = this.entradas
      .filter(e => e.file)
      .map(e => ({ file: e.file!, hoja: e.hoja || undefined }));

    this.importando.set(true);
    this.importResult.set(null);

    this.catalogoService
      .importarBd(this.existenciasFile, this.existenciasHoja || undefined, entradasValidas, this.estrategia)
      .subscribe({
        next: r => {
          this.importResult.set(r);
          this.dbTieneLibros.set(true);
          this.notif.show(`Importación: ${r.creados} creados, ${r.preciosActualizados} actualizados.`, 'success');
          this.importando.set(false);
        },
        error: err => {
          this.notif.show('Error al importar a la base de datos.', 'error');
          console.error(err);
          this.importando.set(false);
        }
      });
  }

  procesar(): void {
    if (!this.existenciasFile || this.cargando()) return;
    const entradasValidas: EntradaItem[] = this.entradas
      .filter(e => e.file)
      .map(e => ({ file: e.file!, hoja: e.hoja || undefined }));

    this.cargando.set(true);
    this.resumen.set(null);

    this.catalogoService
      .procesar(this.existenciasFile, this.existenciasHoja || undefined, entradasValidas, this.estrategia)
      .subscribe({
        next: r => {
          this.resumen.set(r);
          this.notif.show(`Proceso: ${r.actualizados} actualizados, ${r.nuevos} nuevos.`, 'success');
          this.cargando.set(false);
        },
        error: err => {
          this.notif.show('Error al procesar. Verifica los archivos.', 'error');
          console.error(err);
          this.cargando.set(false);
        }
      });
  }
}
