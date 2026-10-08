using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogSync.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionSolicitudIndiceUnicoPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificacionesSolicitud_UsuarioId_LibroId_Estado",
                table: "NotificacionesSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesSolicitud_UsuarioId",
                table: "NotificacionesSolicitud",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesSolicitud_UsuarioId_LibroId_Pendiente",
                table: "NotificacionesSolicitud",
                columns: new[] { "UsuarioId", "LibroId" },
                unique: true,
                filter: "[Estado] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificacionesSolicitud_UsuarioId",
                table: "NotificacionesSolicitud");

            migrationBuilder.DropIndex(
                name: "IX_NotificacionesSolicitud_UsuarioId_LibroId_Pendiente",
                table: "NotificacionesSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesSolicitud_UsuarioId_LibroId_Estado",
                table: "NotificacionesSolicitud",
                columns: new[] { "UsuarioId", "LibroId", "Estado" });
        }
    }
}
