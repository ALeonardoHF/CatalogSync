import { Component, inject, signal, computed, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe } from '@angular/common';
import { LibroService, BulkPortadasResult, BulkPortadasItem } from '../../../core/services/libro.service';
import { LibroDto } from '../../../core/models/libro.models';

@Component({
  selector: 'app-libros',
  standalone: true,
  imports: [ReactiveFormsModule, DecimalPipe, NgClass, TitleCasePipe, LowerCasePipe],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Gestión de Libros</h2>
        <div class="header-actions">
          <button class="btn-outline-danger" (click)="desactivarAgotados()" [disabled]="bulkLoading()">
            Desactivar agotados
          </button>
          <button class="btn-outline-accent" (click)="showPortadasBulk.set(true)">🖼 Portadas masivas</button>
          <button class="btn-primary" (click)="abrirFormulario()">+ Nuevo libro</button>
        </div>
      </div>

      <!-- Barra de búsqueda + filtros -->
      <div class="search-bar">
        <input type="search" placeholder="Buscar por ISBN, título, autor, editorial..."
               (input)="onSearch($event)" />
      </div>

      <div class="filtros-row">
        <div class="filtro-grupo">
          <span class="filtro-label">Estado</span>
          @for (f of filtrosEstado; track f.valor) {
            <button class="chip" [class.active]="filtroEstado() === f.valor"
                    (click)="setFiltroEstado(f.valor)">{{ f.label }}</button>
          }
        </div>
        <div class="filtro-grupo">
          <span class="filtro-label">Inventario</span>
          @for (f of filtrosInv; track f.valor) {
            <button class="chip" [class.active]="filtroInv() === f.valor"
                    (click)="setFiltroInv(f.valor)">{{ f.label }}</button>
          }
        </div>
      </div>

      <!-- Barra de acciones bulk (visible cuando hay seleccionados) -->
      @if (seleccionados().size > 0) {
        <div class="bulk-bar">
          <span class="bulk-count">{{ seleccionados().size }} seleccionado{{ seleccionados().size === 1 ? '' : 's' }}</span>
          <div class="bulk-actions">
            <button class="btn-bulk-success" (click)="bulkAccion('activar')" [disabled]="bulkLoading()">
              Activar seleccionados
            </button>
            <button class="btn-bulk-danger" (click)="bulkAccion('desactivar')" [disabled]="bulkLoading()">
              Desactivar seleccionados
            </button>
            <button class="btn-bulk-clear" (click)="limpiarSeleccion()">✕</button>
          </div>
        </div>
      }

      @if (bulkMsj()) {
        <div class="bulk-toast" [ngClass]="bulkMsjTipo()">{{ bulkMsj() }}</div>
      }

      <!-- Tabla -->
      @if (loading()) {
        <p class="loading">Cargando...</p>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead>
              <tr>
                <th class="col-check">
                  <input type="checkbox" [checked]="todosSeleccionados()"
                         [indeterminate]="algunoSeleccionado() && !todosSeleccionados()"
                         (change)="toggleTodos($event)" />
                </th>
                <th>ISBN</th>
                <th>Título</th>
                <th>Autor</th>
                <th>Editorial</th>
                <th>Precio</th>
                <th>Exist.</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (libro of libros(); track libro.id) {
                <tr [ngClass]="{ 'inactivo': !libro.isActive, 'selected': seleccionados().has(libro.id) }">
                  <td class="col-check">
                    <input type="checkbox" [checked]="seleccionados().has(libro.id)"
                           (change)="toggleSeleccion(libro.id)" />
                  </td>
                  <td>{{ libro.isbn }}</td>
                  <td class="titulo-cell">{{ libro.titulo }}</td>
                  <td>{{ libro.autor }}</td>
                  <td>{{ libro.editorial }}</td>
                  <td class="precio">\${{ libro.precioVenta | number:'1.2-2' }}</td>
                  <td class="center">{{ libro.existencia ?? 0 }}</td>
                  <td>
                    <span class="badge" [ngClass]="badgeClass(libro)">
                      {{ libro.estadoInventario ?? 'N/A' }}
                    </span>
                  </td>
                  <td class="actions">
                    <button class="btn-sm" (click)="editarPrecio(libro)">Precio</button>
                    <button class="btn-sm" (click)="editarInventario(libro)">Inv.</button>
                    <button class="btn-sm img" (click)="iniciarSubidaPortada(libro)"
                            [class.uploading]="subiendoPortadaId() === libro.id"
                            [title]="libro.portada ? 'Cambiar portada' : 'Subir portada'">
                      {{ subiendoPortadaId() === libro.id ? '...' : '🖼' }}
                    </button>
                    @if (libro.isActive) {
                      <button class="btn-sm danger" (click)="toggleActivo(libro)">Desact.</button>
                    } @else {
                      <button class="btn-sm success" (click)="toggleActivo(libro)">Activar</button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="9" class="empty">Sin resultados</td></tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Paginación -->
        <div class="pagination">
          <button [disabled]="page() === 1" (click)="cambiarPagina(page() - 1)">‹</button>
          <span>Página {{ page() }} de {{ totalPages() }}</span>
          <button [disabled]="page() >= totalPages()" (click)="cambiarPagina(page() + 1)">›</button>
          <span class="total">{{ total() }} libros</span>
        </div>
      }

      <!-- Input oculto para portada individual -->
      <input type="file" #portadaInput accept="image/jpeg,image/png,image/webp"
             style="display:none" (change)="onPortadaChange($event)" />

      <!-- Modal Portadas Masivas -->
      @if (showPortadasBulk()) {
        <div class="modal-backdrop" (click)="cerrarPortadasBulk()">
          <div class="modal modal-bulk" (click)="$event.stopPropagation()">
            <div class="modal-head">
              <span class="modal-icon">🖼</span>
              <div>
                <h3>Portadas masivas</h3>
                <p class="modal-subtitle">Nombra cada imagen con el ISBN del libro (ej: 9789706512454.jpg)</p>
              </div>
            </div>

            @if (!bulkPortadaResult()) {
              <div class="bulk-drop-hint">
                <input type="file" #bulkPortadaInput accept="image/jpeg,image/png,image/webp"
                       multiple style="display:none" (change)="onBulkPortadaChange($event)" />
                <button class="btn-select-files" (click)="bulkPortadaInput.click()">
                  Seleccionar imágenes
                </button>
                <span class="drop-hint-text">JPG, PNG o WEBP · máx. 8 MB por imagen</span>
              </div>

              @if (bulkPreview().length > 0) {
                <div class="bulk-preview-list">
                  @for (item of bulkPreview(); track item.nombre) {
                    <div class="bulk-preview-item">
                      <img [src]="item.thumb" [alt]="item.isbn" class="bulk-thumb" />
                      <div class="bulk-item-info">
                        <span class="bulk-isbn">{{ item.isbn }}</span>
                        <span class="bulk-fname">{{ item.nombre }}</span>
                      </div>
                      <button class="btn-remove-preview" (click)="quitarDePreview(item.nombre)">✕</button>
                    </div>
                  }
                </div>
                <p class="bulk-count">{{ bulkPreview().length }} imagen{{ bulkPreview().length === 1 ? '' : 'es' }} seleccionada{{ bulkPreview().length === 1 ? '' : 's' }}</p>
              }

              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="cerrarPortadasBulk()">Cancelar</button>
                <button class="btn-primary" [disabled]="bulkPreview().length === 0 || bulkPortadaLoading()"
                        (click)="subirPortadasBulk()">
                  {{ bulkPortadaLoading() ? 'Subiendo...' : 'Subir ' + bulkPreview().length + ' portada' + (bulkPreview().length === 1 ? '' : 's') }}
                </button>
              </div>
            } @else {
              <!-- Resultado -->
              <div class="bulk-result-stats">
                <div class="brs brs-ok">
                  <span class="brs-num">{{ bulkPortadaResult()!.asignadas }}</span>
                  <span class="brs-label">asignadas</span>
                </div>
                <div class="brs brs-warn">
                  <span class="brs-num">{{ bulkPortadaResult()!.noEncontradas }}</span>
                  <span class="brs-label">sin coincidencia</span>
                </div>
                @if (bulkPortadaResult()!.errores > 0) {
                  <div class="brs brs-err">
                    <span class="brs-num">{{ bulkPortadaResult()!.errores }}</span>
                    <span class="brs-label">errores</span>
                  </div>
                }
              </div>

              <div class="bulk-result-list">
                @for (d of bulkPortadaResult()!.detalles; track d.archivo) {
                  <div class="bulk-result-item" [class]="'res-' + d.resultado.toLowerCase()">
                    <span class="res-icon">{{ d.resultado === 'OK' ? '✓' : '✗' }}</span>
                    <div class="res-info">
                      <span class="res-isbn">{{ d.isbn || d.archivo }}</span>
                      @if (d.titulo) { <span class="res-titulo">{{ d.titulo }}</span> }
                      @if (d.resultado !== 'OK') {
                        <span class="res-motivo">{{
                          d.resultado === 'NoEncontrado'    ? 'ISBN no encontrado en la BD' :
                          d.resultado === 'ExtensionNoValida' ? 'Formato no válido' :
                          d.resultado === 'DemasiadoGrande'   ? 'Archivo mayor a 8 MB' : d.resultado
                        }}</span>
                      }
                    </div>
                  </div>
                }
              </div>

              <div class="modal-footer">
                <button class="btn-secondary" (click)="cerrarPortadasBulk()">Cerrar</button>
                <button class="btn-primary" (click)="bulkPortadaResult.set(null)">Subir más</button>
              </div>
            }
          </div>
        </div>
      }

      <!-- Modal Nuevo/Editar libro -->
      @if (showForm()) {
        <div class="modal-backdrop" (click)="cerrarFormulario()">
          <div class="modal" (click)="$event.stopPropagation()">
            <h3>Nuevo Libro</h3>
            <form [formGroup]="form" (ngSubmit)="guardar()">
              <div class="form-grid">
                <div class="field">
                  <label>ISBN *</label>
                  <input formControlName="isbn" placeholder="9781234567890" />
                </div>
                <div class="field">
                  <label>Código de barra</label>
                  <input formControlName="codigoBarra" />
                </div>
                <div class="field span2">
                  <label>Título *</label>
                  <input formControlName="titulo" />
                </div>
                <div class="field">
                  <label>Autor *</label>
                  <input formControlName="autor" />
                </div>
                <div class="field">
                  <label>Editorial *</label>
                  <input formControlName="editorial" />
                </div>
                <div class="field">
                  <label>Precio venta *</label>
                  <input formControlName="precioVenta" type="number" step="0.01" min="0" />
                </div>
                <div class="field">
                  <label>Costo</label>
                  <input formControlName="costo" type="number" step="0.01" min="0" />
                </div>
                <div class="field">
                  <label>Descuento %</label>
                  <input formControlName="descuento" type="number" step="0.01" min="0" max="100" />
                </div>
                <div class="field">
                  <label>Género</label>
                  <input formControlName="genero" />
                </div>
                <div class="field">
                  <label>Páginas</label>
                  <input formControlName="paginas" type="number" min="1" />
                </div>
                <div class="field">
                  <label>Año publicación</label>
                  <input formControlName="anioPublicacion" type="number" />
                </div>
                <div class="field span2">
                  <label>URL Portada</label>
                  <input formControlName="portada" />
                </div>
              </div>

              @if (formError()) {
                <div class="alert-error">{{ formError() }}</div>
              }

              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="cerrarFormulario()">Cancelar</button>
                <button type="submit" class="btn-primary" [disabled]="form.invalid || saving()">
                  @if (saving()) { Guardando... } @else { Guardar }
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Modal Precio -->
      @if (libroEditandoPrecio()) {
        <div class="modal-backdrop" (click)="libroEditandoPrecio.set(null)">
          <div class="modal modal-sm" (click)="$event.stopPropagation()">
            <div class="modal-head">
              <span class="modal-icon">$</span>
              <div>
                <h3>Actualizar precio</h3>
                <p class="modal-subtitle">{{ libroEditandoPrecio()!.titulo | lowercase | titlecase }}</p>
              </div>
            </div>
            <form [formGroup]="precioForm" (ngSubmit)="guardarPrecio()">
              <div class="field-row">
                <div class="field">
                  <label>Precio de venta *</label>
                  <div class="input-prefix">
                    <span>$</span>
                    <input formControlName="precioVenta" type="number" step="0.01" min="0" />
                  </div>
                </div>
                <div class="field">
                  <label>Costo</label>
                  <div class="input-prefix">
                    <span>$</span>
                    <input formControlName="costo" type="number" step="0.01" min="0" />
                  </div>
                </div>
              </div>
              <div class="field">
                <label>Fuente / motivo</label>
                <input formControlName="fuente" placeholder="Proveedor, ajuste manual..." />
              </div>
              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="libroEditandoPrecio.set(null)">Cancelar</button>
                <button type="submit" class="btn-primary" [disabled]="precioForm.invalid || saving()">
                  {{ saving() ? 'Guardando...' : 'Actualizar' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Modal Inventario -->
      @if (libroEditandoInv()) {
        <div class="modal-backdrop" (click)="libroEditandoInv.set(null)">
          <div class="modal modal-sm" (click)="$event.stopPropagation()">
            <div class="modal-head">
              <span class="modal-icon">#</span>
              <div>
                <h3>Actualizar inventario</h3>
                <p class="modal-subtitle">{{ libroEditandoInv()!.titulo | lowercase | titlecase }}</p>
              </div>
            </div>
            <form [formGroup]="invForm" (ngSubmit)="guardarInventario()">
              <div class="field">
                <label>Existencia actual *</label>
                <input formControlName="existencia" type="number" min="0" />
              </div>
              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="libroEditandoInv.set(null)">Cancelar</button>
                <button type="submit" class="btn-primary" [disabled]="invForm.invalid || saving()">
                  {{ saving() ? 'Guardando...' : 'Actualizar' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    /* Layout */
    .page-header {
      display: flex; justify-content: space-between; align-items: center;
      margin-bottom: 1.25rem;
    }
    h2 { margin: 0; font-size: 1.3rem; color: var(--text); }
    .header-actions { display: flex; gap: .6rem; align-items: center; }

    /* Filtros */
    .filtros-row {
      display: flex; gap: 1.5rem; flex-wrap: wrap;
      margin-bottom: 1.25rem; align-items: center;
    }
    .filtro-grupo { display: flex; align-items: center; gap: .4rem; }
    .filtro-label { font-size: .72rem; color: var(--muted); text-transform: uppercase; letter-spacing: .05em; margin-right: .2rem; }
    .chip {
      all: unset; cursor: pointer;
      padding: .25rem .75rem; border: 1px solid var(--border);
      border-radius: 20px; font-size: .8rem; color: var(--muted);
      transition: all .15s;
    }
    .chip:hover { border-color: var(--accent); color: var(--accent); }
    .chip.active { border-color: var(--primary); background: var(--primary); color: #fff; }

    /* Bulk bar */
    .bulk-bar {
      display: flex; align-items: center; gap: 1rem;
      padding: .65rem 1rem; margin-bottom: .75rem;
      background: #1e1e3a; border: 1px solid var(--primary);
      border-radius: 8px;
    }
    .bulk-count { font-size: .88rem; color: var(--accent); font-weight: 600; white-space: nowrap; }
    .bulk-actions { display: flex; gap: .5rem; margin-left: auto; }
    .btn-bulk-success {
      all: unset; cursor: pointer;
      padding: .3rem .85rem; border: 1px solid #4ade8060; color: var(--success);
      border-radius: 6px; font-size: .82rem; font-weight: 500;
      transition: background .15s, border-color .15s;
    }
    .btn-bulk-success:hover:not(:disabled) { background: #4ade8015; border-color: var(--success); }
    .btn-bulk-success:disabled { opacity: .4; cursor: not-allowed; }
    .btn-bulk-danger {
      all: unset; cursor: pointer;
      padding: .3rem .85rem; border: 1px solid #f8717160; color: var(--error);
      border-radius: 6px; font-size: .82rem; font-weight: 500;
      transition: background .15s, border-color .15s;
    }
    .btn-bulk-danger:hover:not(:disabled) { background: #f8717115; border-color: var(--error); }
    .btn-bulk-danger:disabled { opacity: .4; cursor: not-allowed; }
    .btn-bulk-clear {
      all: unset; cursor: pointer;
      padding: .3rem .65rem; border: 1px solid var(--border); color: var(--muted);
      border-radius: 6px; font-size: .82rem; transition: border-color .15s, color .15s;
    }
    .btn-bulk-clear:hover { border-color: var(--accent); color: var(--accent); }

    /* Bulk toast */
    .bulk-toast {
      padding: .55rem .9rem; border-radius: 6px; margin-bottom: .75rem;
      font-size: .85rem;
    }
    .bulk-toast.success { background: #4ade8015; color: var(--success); border: 1px solid #4ade8030; }
    .bulk-toast.error   { background: #f8717115; color: var(--error);   border: 1px solid #f8717130; }

    /* Buscador */
    .search-bar { margin-bottom: 1rem; }
    .search-bar input {
      width: 100%; padding: .65rem .85rem;
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 8px; color: var(--text); font-size: .95rem;
      box-sizing: border-box;
    }
    .search-bar input:focus { outline: none; border-color: var(--primary); }

    /* Tabla */
    .table-wrapper {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 10px; overflow: hidden;
    }
    tr.inactivo td { opacity: .4; }
    tr.selected { background: #1e1e3a !important; }
    .col-check { width: 36px; text-align: center; }
    .col-check input[type=checkbox] { cursor: pointer; accent-color: var(--primary); }
    .titulo-cell { max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .precio { font-weight: 600; color: var(--success); }
    .center { text-align: center; }

    /* Acciones en tabla */
    .actions { display: flex; gap: .35rem; white-space: nowrap; align-items: center; }
    .btn-sm {
      all: unset; cursor: pointer;
      padding: .2rem .55rem; font-size: .78rem; border-radius: 5px;
      border: 1px solid var(--border); color: var(--muted);
      transition: border-color .15s, color .15s, background .15s;
      white-space: nowrap;
    }
    .btn-sm:hover { border-color: var(--accent); color: var(--accent); }
    .btn-sm.img { border-color: #a855f740; color: var(--accent); }
    .btn-sm.img:hover { border-color: var(--accent); background: #a855f715; }
    .btn-sm.uploading { opacity: .5; cursor: not-allowed; }
    .btn-sm.danger  { border-color: #f8717140; color: var(--error); }
    .btn-sm.danger:hover { border-color: var(--error); background: #f8717110; }
    .btn-sm.success { border-color: #4ade8040; color: var(--success); }
    .btn-sm.success:hover { border-color: var(--success); background: #4ade8010; }

    /* Badges estado */
    .badge { padding: .2rem .6rem; border-radius: 20px; font-size: .72rem; font-weight: 600; }
    .badge-disponible { background: #4ade8018; color: var(--success); border: 1px solid #4ade8030; }
    .badge-agotado    { background: #f8717118; color: var(--error);   border: 1px solid #f8717130; }
    .badge-default    { background: #6b6b8a18; color: var(--muted);   border: 1px solid var(--border); }

    /* Paginación */
    .pagination {
      display: flex; align-items: center; gap: .75rem;
      margin-top: 1rem; font-size: .88rem; padding: .5rem .25rem;
    }
    .pagination button {
      all: unset; cursor: pointer;
      padding: .3rem .65rem; border: 1px solid var(--border);
      border-radius: 5px; color: var(--muted);
      transition: border-color .15s, color .15s;
    }
    .pagination button:hover:not(:disabled) { border-color: var(--accent); color: var(--accent); }
    .pagination button:disabled { opacity: .3; cursor: not-allowed; }
    .total { margin-left: auto; color: var(--muted); font-size: .83rem; }
    .loading, .empty { text-align: center; color: var(--muted); padding: 2.5rem; }

    /* Botones header */
    .btn-primary {
      all: unset; cursor: pointer;
      padding: .55rem 1.25rem; background: var(--primary); color: #fff;
      border-radius: 8px; font-size: .9rem; font-weight: 500;
      transition: opacity .15s;
    }
    .btn-primary:hover:not(:disabled) { opacity: .85; }
    .btn-primary:disabled { opacity: .4; cursor: not-allowed; }

    .btn-secondary {
      all: unset; cursor: pointer;
      padding: .55rem 1.25rem; color: var(--muted);
      border: 1px solid var(--border); border-radius: 8px; font-size: .9rem;
      transition: border-color .15s, color .15s;
    }
    .btn-secondary:hover { border-color: var(--accent); color: var(--accent); }

    .btn-outline-accent {
      all: unset; cursor: pointer;
      padding: .5rem 1.1rem; color: var(--accent);
      border: 1px solid #a855f750; border-radius: 8px; font-size: .87rem;
      transition: border-color .15s, background .15s;
    }
    .btn-outline-accent:hover { border-color: var(--accent); background: #a855f710; }

    .btn-outline-danger {
      all: unset; cursor: pointer;
      padding: .5rem 1.1rem; color: var(--error);
      border: 1px solid #f8717150; border-radius: 8px; font-size: .87rem;
      transition: border-color .15s, background .15s;
    }
    .btn-outline-danger:hover:not(:disabled) { border-color: var(--error); background: #f8717110; }
    .btn-outline-danger:disabled { opacity: .4; cursor: not-allowed; }

    /* Form fields */
    .field { display: flex; flex-direction: column; gap: .35rem; }
    .field label { font-size: .8rem; color: var(--muted); }
    .field input {
      background: #0a0a18; border: 1px solid var(--border); border-radius: 8px;
      padding: .55rem .7rem; color: var(--text); font-size: .93rem;
      transition: border-color .2s;
    }
    .field input:focus { outline: none; border-color: var(--primary); }

    /* Modal */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,.65);
      display: flex; align-items: center; justify-content: center; z-index: 100;
    }
    .modal {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 16px; padding: 1.5rem;
      width: 600px; max-width: 95vw; max-height: 90vh; overflow-y: auto;
    }
    .modal-sm { width: 400px; }

    /* Encabezado del modal */
    .modal-head {
      display: flex; align-items: flex-start; gap: .85rem;
      margin-bottom: 1.5rem;
    }
    .modal-icon {
      flex-shrink: 0;
      width: 38px; height: 38px; border-radius: 10px;
      background: var(--primary); color: #fff;
      display: flex; align-items: center; justify-content: center;
      font-size: 1.1rem; font-weight: 700;
    }
    .modal-head h3 { margin: 0 0 .2rem; font-size: 1rem; color: var(--text); }
    .modal-subtitle {
      margin: 0; font-size: .8rem; color: var(--muted);
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
      max-width: 300px;
    }

    .modal h3 { margin: 0 0 1.25rem; font-size: 1.05rem; color: var(--text); }

    /* Campos en fila */
    .field-row { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; margin-bottom: .75rem; }

    /* Input con prefijo */
    .input-prefix {
      display: flex; align-items: center;
      background: #0a0a18; border: 1px solid var(--border); border-radius: 8px;
      overflow: hidden; transition: border-color .2s;
    }
    .input-prefix:focus-within { border-color: var(--primary); }
    .input-prefix span {
      padding: 0 .65rem; color: var(--muted); font-size: .9rem;
      border-right: 1px solid var(--border); user-select: none;
    }
    .input-prefix input {
      background: transparent; border: none; outline: none;
      padding: .55rem .7rem; color: var(--text); font-size: .93rem;
      width: 100%;
    }
    .input-prefix input:focus { border-color: transparent; box-shadow: none; }

    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; }
    .span2 { grid-column: span 2; }
    .modal-footer {
      display: flex; justify-content: flex-end; gap: .75rem;
      margin-top: 1.5rem; padding-top: 1rem;
      border-top: 1px solid var(--border);
    }
    /* Modal portadas masivas */
    .modal-bulk { width: 560px; max-height: 85vh; overflow-y: auto; }

    .bulk-drop-hint {
      display: flex; align-items: center; gap: 1rem;
      padding: 1.25rem; border: 1.5px dashed var(--border); border-radius: 10px;
      margin-bottom: 1rem;
    }
    .btn-select-files {
      all: unset; cursor: pointer;
      padding: .5rem 1.2rem; background: var(--primary); color: #fff;
      border-radius: 7px; font-size: .87rem; font-weight: 500; white-space: nowrap;
      transition: opacity .15s;
    }
    .btn-select-files:hover { opacity: .85; }
    .drop-hint-text { font-size: .8rem; color: var(--muted); }

    .bulk-preview-list {
      display: flex; flex-direction: column; gap: .4rem;
      max-height: 280px; overflow-y: auto;
      border: 1px solid var(--border); border-radius: 8px; padding: .5rem;
      margin-bottom: .5rem;
    }
    .bulk-preview-item {
      display: flex; align-items: center; gap: .75rem;
      padding: .35rem .5rem; border-radius: 6px;
      background: #0a0a18;
    }
    .bulk-thumb {
      width: 36px; height: 50px; object-fit: cover;
      border-radius: 3px; flex-shrink: 0;
      border: 1px solid var(--border);
    }
    .bulk-item-info { flex: 1; min-width: 0; }
    .bulk-isbn { display: block; font-size: .82rem; font-weight: 600; color: var(--text); font-family: monospace; }
    .bulk-fname { display: block; font-size: .72rem; color: var(--muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .btn-remove-preview {
      all: unset; cursor: pointer; color: var(--muted); font-size: .85rem; padding: .2rem .4rem;
      border-radius: 4px; transition: color .15s;
    }
    .btn-remove-preview:hover { color: var(--error); }
    .bulk-count { font-size: .82rem; color: var(--muted); margin: 0 0 .5rem; }

    /* Resultado */
    .bulk-result-stats {
      display: flex; gap: .75rem; margin-bottom: 1rem; flex-wrap: wrap;
    }
    .brs {
      flex: 1; min-width: 90px; text-align: center;
      padding: .75rem .5rem; border-radius: 8px; border: 1px solid;
    }
    .brs-ok   { background: #4ade8012; border-color: #4ade8030; }
    .brs-warn { background: #fbbf2412; border-color: #fbbf2430; }
    .brs-err  { background: #f8717112; border-color: #f8717130; }
    .brs-num  { display: block; font-size: 1.6rem; font-weight: 700; }
    .brs-ok   .brs-num  { color: var(--success); }
    .brs-warn .brs-num  { color: #fbbf24; }
    .brs-err  .brs-num  { color: var(--error); }
    .brs-label { font-size: .75rem; color: var(--muted); }

    .bulk-result-list {
      display: flex; flex-direction: column; gap: .3rem;
      max-height: 260px; overflow-y: auto;
      border: 1px solid var(--border); border-radius: 8px; padding: .5rem;
    }
    .bulk-result-item {
      display: flex; align-items: flex-start; gap: .6rem;
      padding: .4rem .5rem; border-radius: 5px; font-size: .83rem;
    }
    .res-ok           { background: #4ade8008; }
    .res-noencontrado { background: #fbbf2408; }
    .res-extensionnovalida, .res-demasiadogrande { background: #f8717108; }
    .res-icon { font-size: .9rem; margin-top: .05rem; flex-shrink: 0; }
    .res-ok   .res-icon { color: var(--success); }
    .res-noencontrado .res-icon,
    .res-extensionnovalida .res-icon,
    .res-demasiadogrande .res-icon { color: var(--error); }
    .res-info { display: flex; flex-direction: column; gap: .1rem; min-width: 0; }
    .res-isbn  { font-family: monospace; font-weight: 600; color: var(--text); }
    .res-titulo { font-size: .78rem; color: var(--muted); }
    .res-motivo { font-size: .75rem; color: var(--error); }

    .alert-error {
      background: #f8717110; color: var(--error);
      border: 1px solid #f8717140; border-radius: 6px;
      padding: .5rem .75rem; margin-top: .75rem; font-size: .875rem;
    }
  `]
})
export class LibrosComponent implements OnInit {
  private readonly svc = inject(LibroService);
  private readonly fb  = inject(FormBuilder);

  readonly libros    = signal<LibroDto[]>([]);
  readonly loading   = signal(false);
  readonly saving    = signal(false);
  readonly bulkLoading = signal(false);
  readonly showForm  = signal(false);
  readonly formError = signal('');
  readonly page      = signal(1);
  readonly total     = signal(0);
  readonly totalPages = signal(1);
  readonly libroEditandoPrecio = signal<LibroDto | null>(null);
  readonly libroEditandoInv    = signal<LibroDto | null>(null);
  readonly seleccionados       = signal<Set<string>>(new Set());
  readonly bulkMsj             = signal('');
  readonly bulkMsjTipo         = signal<'success' | 'error'>('success');
  readonly subiendoPortadaId   = signal<string | null>(null);

  // Portadas masivas
  readonly showPortadasBulk    = signal(false);
  readonly bulkPortadaLoading  = signal(false);
  readonly bulkPortadaResult   = signal<BulkPortadasResult | null>(null);
  readonly bulkPreview         = signal<{ nombre: string; isbn: string; thumb: string; file: File }[]>([]);

  @ViewChild('portadaInput') portadaInput!: ElementRef<HTMLInputElement>;
  private libroSubiendoPortada: LibroDto | null = null;

  private searchTerm = '';
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filtroEstado = signal<'activo' | 'inactivo' | 'todos'>('activo');
  readonly filtroInv    = signal<'todos' | 'conExistencia' | 'sinExistencia'>('todos');

  readonly filtrosEstado = [
    { valor: 'activo'   as const, label: 'Activos' },
    { valor: 'inactivo' as const, label: 'Desactivados' },
    { valor: 'todos'    as const, label: 'Todos' }
  ];
  readonly filtrosInv = [
    { valor: 'todos'          as const, label: 'Todos' },
    { valor: 'conExistencia'  as const, label: 'Con existencia' },
    { valor: 'sinExistencia'  as const, label: 'Agotados' }
  ];

  readonly todosSeleccionados = computed(() => {
    const ids = this.libros().map(l => l.id);
    return ids.length > 0 && ids.every(id => this.seleccionados().has(id));
  });

  readonly algunoSeleccionado = computed(() =>
    this.libros().some(l => this.seleccionados().has(l.id))
  );

  readonly form = this.fb.nonNullable.group({
    isbn:           ['', Validators.required],
    titulo:         ['', Validators.required],
    autor:          ['', Validators.required],
    editorial:      ['', Validators.required],
    precioVenta:    [0, [Validators.required, Validators.min(0)]],
    costo:          [0],
    descuento:      [0],
    genero:         [''],
    paginas:        [null as number | null],
    anioPublicacion:[null as number | null],
    portada:        [''],
    codigoBarra:    ['']
  });

  readonly precioForm = this.fb.nonNullable.group({
    precioVenta: [0, [Validators.required, Validators.min(0)]],
    costo:       [null as number | null],
    fuente:      ['']
  });

  readonly invForm = this.fb.nonNullable.group({
    existencia: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void { this.cargar(); }

  setFiltroEstado(v: 'activo' | 'inactivo' | 'todos'): void {
    this.filtroEstado.set(v); this.page.set(1); this.limpiarSeleccion(); this.cargar();
  }
  setFiltroInv(v: 'todos' | 'conExistencia' | 'sinExistencia'): void {
    this.filtroInv.set(v); this.page.set(1); this.limpiarSeleccion(); this.cargar();
  }

  private get isActiveParam(): boolean | null {
    const e = this.filtroEstado();
    if (e === 'activo')   return true;
    if (e === 'inactivo') return false;
    return null;
  }

  cargar(): void {
    this.loading.set(true);
    const soloConExistencia = this.filtroInv() === 'conExistencia';
    const sinExistencia     = this.filtroInv() === 'sinExistencia';
    this.svc.buscar(this.searchTerm || undefined, this.page(), 20, soloConExistencia, this.isActiveParam, sinExistencia).subscribe({
      next: r => {
        this.libros.set(r.items);
        this.total.set(r.total);
        this.totalPages.set(r.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.searchTerm = value;
      this.page.set(1);
      this.limpiarSeleccion();
      this.cargar();
    }, 350);
  }

  cambiarPagina(p: number): void {
    this.page.set(p);
    this.limpiarSeleccion();
    this.cargar();
  }

  // ── Selección ──────────────────────────────────────────────────────────────

  toggleSeleccion(id: string): void {
    this.seleccionados.update(s => {
      const n = new Set(s);
      if (n.has(id)) n.delete(id); else n.add(id);
      return n;
    });
  }

  toggleTodos(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.seleccionados.update(s => {
        const n = new Set(s);
        this.libros().forEach(l => n.add(l.id));
        return n;
      });
    } else {
      this.seleccionados.update(s => {
        const n = new Set(s);
        this.libros().forEach(l => n.delete(l.id));
        return n;
      });
    }
  }

  limpiarSeleccion(): void {
    this.seleccionados.set(new Set());
  }

  // ── Acciones bulk ─────────────────────────────────────────────────────────

  bulkAccion(accion: 'activar' | 'desactivar'): void {
    const ids = [...this.seleccionados()];
    if (ids.length === 0) return;
    this.bulkLoading.set(true);
    this.svc.bulkAccion(ids, accion).subscribe({
      next: r => {
        this.bulkLoading.set(false);
        this.limpiarSeleccion();
        this.mostrarBulkMsj(`${r.afectados} libro${r.afectados === 1 ? '' : 's'} ${accion === 'activar' ? 'activado' : 'desactivado'}${r.afectados === 1 ? '' : 's'}`, 'success');
        this.cargar();
      },
      error: () => { this.bulkLoading.set(false); this.mostrarBulkMsj('Error al procesar la acción', 'error'); }
    });
  }

  desactivarAgotados(): void {
    this.bulkLoading.set(true);
    this.svc.desactivarAgotados().subscribe({
      next: r => {
        this.bulkLoading.set(false);
        if (r.afectados === 0) {
          this.mostrarBulkMsj('No hay libros activos sin existencia', 'success');
        } else {
          this.mostrarBulkMsj(`${r.afectados} libro${r.afectados === 1 ? '' : 's'} agotado${r.afectados === 1 ? '' : 's'} desactivado${r.afectados === 1 ? '' : 's'}`, 'success');
          this.cargar();
        }
      },
      error: () => { this.bulkLoading.set(false); this.mostrarBulkMsj('Error al desactivar agotados', 'error'); }
    });
  }

  // ── Portadas masivas ──────────────────────────────────────────────────────

  onBulkPortadaChange(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    const current = this.bulkPreview();
    const nuevos = files
      .filter(f => !current.some(p => p.nombre === f.name))
      .map(f => ({
        nombre: f.name,
        isbn: f.name.replace(/\.[^/.]+$/, ''),
        thumb: URL.createObjectURL(f),
        file: f
      }));
    this.bulkPreview.update(prev => [...prev, ...nuevos]);
  }

  quitarDePreview(nombre: string): void {
    this.bulkPreview.update(prev => {
      const item = prev.find(p => p.nombre === nombre);
      if (item) URL.revokeObjectURL(item.thumb);
      return prev.filter(p => p.nombre !== nombre);
    });
  }

  subirPortadasBulk(): void {
    const items = this.bulkPreview();
    if (items.length === 0) return;
    this.bulkPortadaLoading.set(true);
    this.svc.subirPortadasBulk(items.map(i => i.file)).subscribe({
      next: result => {
        this.bulkPortadaLoading.set(false);
        this.bulkPortadaResult.set(result);
        this.bulkPreview().forEach(p => URL.revokeObjectURL(p.thumb));
        this.bulkPreview.set([]);
        if (result.asignadas > 0) this.cargar();
      },
      error: () => {
        this.bulkPortadaLoading.set(false);
        this.mostrarBulkMsj('Error al subir las portadas', 'error');
      }
    });
  }

  cerrarPortadasBulk(): void {
    this.bulkPreview().forEach(p => URL.revokeObjectURL(p.thumb));
    this.bulkPreview.set([]);
    this.bulkPortadaResult.set(null);
    this.showPortadasBulk.set(false);
  }

  iniciarSubidaPortada(libro: LibroDto): void {
    if (this.subiendoPortadaId() === libro.id) return;
    this.libroSubiendoPortada = libro;
    this.portadaInput.nativeElement.value = '';
    this.portadaInput.nativeElement.click();
  }

  onPortadaChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    const libro = this.libroSubiendoPortada;
    if (!file || !libro) return;
    this.subiendoPortadaId.set(libro.id);
    this.svc.subirPortada(libro.id, file).subscribe({
      next: ({ url }) => {
        this.subiendoPortadaId.set(null);
        this.mostrarBulkMsj('Portada actualizada', 'success');
        this.libros.update(items => items.map(l => l.id === libro.id ? { ...l, portada: url } : l));
      },
      error: (err) => {
        this.subiendoPortadaId.set(null);
        this.mostrarBulkMsj(err.error?.message ?? 'Error al subir la imagen', 'error');
      }
    });
  }

  private mostrarBulkMsj(msj: string, tipo: 'success' | 'error'): void {
    this.bulkMsj.set(msj);
    this.bulkMsjTipo.set(tipo);
    setTimeout(() => this.bulkMsj.set(''), 4000);
  }

  // ── CRUD ───────────────────────────────────────────────────────────────────

  abrirFormulario(): void {
    this.form.reset({ precioVenta: 0, costo: 0, descuento: 0 });
    this.formError.set('');
    this.showForm.set(true);
  }

  cerrarFormulario(): void { this.showForm.set(false); }

  guardar(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set('');
    const v = this.form.getRawValue();

    this.svc.crear({
      isbn: v.isbn, titulo: v.titulo, autor: v.autor, editorial: v.editorial,
      precioVenta: v.precioVenta, costo: v.costo, descuento: v.descuento,
      genero: v.genero || undefined, paginas: v.paginas ?? undefined,
      anioPublicacion: v.anioPublicacion ?? undefined,
      portada: v.portada || undefined, codigoBarra: v.codigoBarra || undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.cerrarFormulario();
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(err.error?.message ?? 'Error al guardar.');
      }
    });
  }

  editarPrecio(libro: LibroDto): void {
    this.precioForm.patchValue({ precioVenta: libro.precioVenta, costo: libro.costo, fuente: '' });
    this.libroEditandoPrecio.set(libro);
  }

  guardarPrecio(): void {
    const libro = this.libroEditandoPrecio();
    if (!libro || this.precioForm.invalid) return;
    this.saving.set(true);
    const v = this.precioForm.getRawValue();

    this.svc.actualizarPrecio(libro.id, {
      precioVenta: v.precioVenta,
      costo: v.costo ?? undefined,
      fuente: v.fuente || undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.libroEditandoPrecio.set(null);
        this.cargar();
      },
      error: () => this.saving.set(false)
    });
  }

  editarInventario(libro: LibroDto): void {
    this.invForm.patchValue({ existencia: libro.existencia ?? 0 });
    this.libroEditandoInv.set(libro);
  }

  guardarInventario(): void {
    const libro = this.libroEditandoInv();
    if (!libro || this.invForm.invalid) return;
    this.saving.set(true);

    this.svc.actualizarInventario(libro.id, { existencia: this.invForm.getRawValue().existencia }).subscribe({
      next: () => {
        this.saving.set(false);
        this.libroEditandoInv.set(null);
        this.cargar();
      },
      error: () => this.saving.set(false)
    });
  }

  toggleActivo(libro: LibroDto): void {
    const obs = libro.isActive ? this.svc.desactivar(libro.id) : this.svc.activar(libro.id);
    obs.subscribe({ next: () => this.cargar() });
  }

  badgeClass(libro: LibroDto): string {
    const estado = (libro.estadoInventario ?? '').toLowerCase();
    if (estado === 'disponible') return 'badge badge-disponible';
    if (estado === 'agotado')    return 'badge badge-agotado';
    return 'badge badge-default';
  }
}
