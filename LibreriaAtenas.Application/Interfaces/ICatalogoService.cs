using LibreriaAtenas.Application.Models;
using LibreriaAtenas.Domain.Enums;

namespace LibreriaAtenas.Application.Interfaces;

public interface ICatalogoService
{
    ResumenProceso ProcesarCatalogo(List<LibroExistencia> catalogo, List<LibroProveedor> librosEntrada, EstrategiaPrecio estrategia = EstrategiaPrecio.MasAltoSiHayExistencia);
    string NormalizarISBN(string isbn);
}
