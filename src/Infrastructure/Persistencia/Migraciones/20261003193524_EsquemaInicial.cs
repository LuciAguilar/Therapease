using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TherapEase.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class EsquemaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'therapease_app') THEN
                        RAISE EXCEPTION 'Falta el rol therapease_app. Ejecuta scripts/db/crear-roles.sql antes de migrar.';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "EventoAuditoria",
                columns: table => new
                {
                    IdEvento = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaEvento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdUsuarioActor = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoRegistro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdRegistro = table.Column<Guid>(type: "uuid", nullable: false),
                    Accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CamposAfectados = table.Column<string[]>(type: "text[]", nullable: false),
                    Hecho = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoAuditoria", x => x.IdEvento);
                });

            migrationBuilder.CreateTable(
                name: "Paciente",
                columns: table => new
                {
                    IdPaciente = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Contacto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Condicion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FechaAlta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdUsuarioAlta = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IdUsuarioActualizacion = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaBaja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IdUsuarioBaja = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paciente", x => x.IdPaciente);
                    table.CheckConstraint("CK_Paciente_BajaCoherente", "(\"Condicion\" = 'Vigente' AND \"FechaBaja\" IS NULL AND \"IdUsuarioBaja\" IS NULL) OR (\"Condicion\" = 'Baja' AND \"FechaBaja\" IS NOT NULL AND \"IdUsuarioBaja\" IS NOT NULL)");
                    table.CheckConstraint("CK_Paciente_NombreNoVacio", "length(btrim(\"Nombre\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Cita",
                columns: table => new
                {
                    IdCita = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPaciente = table.Column<Guid>(type: "uuid", nullable: false),
                    Ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EstadoPago = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Condicion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FechaAlta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdUsuarioAlta = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IdUsuarioActualizacion = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaBaja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IdUsuarioBaja = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cita", x => x.IdCita);
                    table.CheckConstraint("CK_Cita_Ambito", "\"Ambito\" IN ('Escolar', 'Independiente')");
                    table.CheckConstraint("CK_Cita_BajaCoherente", "(\"Condicion\" = 'Vigente' AND \"FechaBaja\" IS NULL AND \"IdUsuarioBaja\" IS NULL) OR (\"Condicion\" = 'Baja' AND \"FechaBaja\" IS NOT NULL AND \"IdUsuarioBaja\" IS NOT NULL)");
                    table.CheckConstraint("CK_Cita_Estado", "\"Estado\" IN ('Agendada', 'Cancelada')");
                    table.CheckConstraint("CK_Cita_EstadoPago", "\"EstadoPago\" IN ('Pendiente', 'Pagado')");
                    table.CheckConstraint("CK_Cita_InicioAntesDeFin", "\"Inicio\" < \"Fin\"");
                    table.ForeignKey(
                        name: "FK_Cita_Paciente_IdPaciente",
                        column: x => x.IdPaciente,
                        principalTable: "Paciente",
                        principalColumn: "IdPaciente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PacienteAmbito",
                columns: table => new
                {
                    IdPaciente = table.Column<Guid>(type: "uuid", nullable: false),
                    Ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacienteAmbito", x => new { x.IdPaciente, x.Ambito });
                    table.CheckConstraint("CK_PacienteAmbito_Ambito", "\"Ambito\" IN ('Escolar', 'Independiente')");
                    table.ForeignKey(
                        name: "FK_PacienteAmbito_Paciente_IdPaciente",
                        column: x => x.IdPaciente,
                        principalTable: "Paciente",
                        principalColumn: "IdPaciente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdPaciente",
                table: "Cita",
                column: "IdPaciente");

            migrationBuilder.CreateIndex(
                name: "IX_EventoAuditoria_FechaEvento",
                table: "EventoAuditoria",
                column: "FechaEvento");

            migrationBuilder.CreateIndex(
                name: "IX_EventoAuditoria_TipoRegistro_IdRegistro_FechaEvento",
                table: "EventoAuditoria",
                columns: new[] { "TipoRegistro", "IdRegistro", "FechaEvento" });

            // Q06: dos citas activas no pueden solaparse; intervalos contiguos [inicio, fin) sí se permiten.
            migrationBuilder.Sql("""
                ALTER TABLE "Cita" ADD CONSTRAINT "EX_Cita_SinCruceActivo"
                    EXCLUDE USING gist (tstzrange("Inicio", "Fin", '[)') WITH &&)
                    WHERE ("Estado" = 'Agendada' AND "Condicion" = 'Vigente');
                """);

            // Una cita activa exige paciente vigente. FOR SHARE bloquea la baja concurrente del paciente hasta confirmar.
            migrationBuilder.Sql("""
                CREATE FUNCTION "FnCitaActivaExigePacienteVigente"() RETURNS trigger
                LANGUAGE plpgsql AS $$
                DECLARE
                    condicion_paciente text;
                BEGIN
                    IF NEW."Estado" = 'Agendada' AND NEW."Condicion" = 'Vigente' THEN
                        SELECT "Condicion" INTO condicion_paciente
                        FROM "Paciente" WHERE "IdPaciente" = NEW."IdPaciente" FOR SHARE;
                        IF FOUND AND condicion_paciente <> 'Vigente' THEN
                            RAISE EXCEPTION 'No puede haber una cita activa de un paciente dado de baja' USING ERRCODE = 'TE001';
                        END IF;
                    END IF;
                    RETURN NEW;
                END $$;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Cita_ActivaExigePacienteVigente"
                BEFORE INSERT OR UPDATE ON "Cita"
                FOR EACH ROW EXECUTE FUNCTION "FnCitaActivaExigePacienteVigente"();
                """);

            // Complemento del anterior: una baja que llega después de una cita activa confirmada se rechaza.
            migrationBuilder.Sql("""
                CREATE FUNCTION "FnPacienteDeBajaSinCitasActivas"() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Cita"
                               WHERE "IdPaciente" = NEW."IdPaciente" AND "Estado" = 'Agendada' AND "Condicion" = 'Vigente') THEN
                        RAISE EXCEPTION 'No puede haber una cita activa de un paciente dado de baja' USING ERRCODE = 'TE001';
                    END IF;
                    RETURN NULL;
                END $$;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Paciente_BajaSinCitasActivas"
                AFTER UPDATE OF "Condicion" ON "Paciente"
                FOR EACH ROW WHEN (NEW."Condicion" = 'Baja' AND OLD."Condicion" IS DISTINCT FROM 'Baja')
                EXECUTE FUNCTION "FnPacienteDeBajaSinCitasActivas"();
                """);

            // Permisos mínimos de therapease_app (Q20, Q03); toda tabla nueva necesita sus GRANT en su migración.
            migrationBuilder.Sql("""
                REVOKE CREATE ON SCHEMA public FROM PUBLIC;
                GRANT USAGE ON SCHEMA public TO therapease_app;
                GRANT SELECT, INSERT, UPDATE ON "Paciente", "Cita" TO therapease_app;
                GRANT SELECT, INSERT, DELETE ON "PacienteAmbito" TO therapease_app;
                GRANT SELECT, INSERT ON "EventoAuditoria" TO therapease_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cita");

            migrationBuilder.DropTable(
                name: "EventoAuditoria");

            migrationBuilder.DropTable(
                name: "PacienteAmbito");

            migrationBuilder.DropTable(
                name: "Paciente");

            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS "FnCitaActivaExigePacienteVigente"();
                DROP FUNCTION IF EXISTS "FnPacienteDeBajaSinCitasActivas"();
                REVOKE USAGE ON SCHEMA public FROM therapease_app;
                """);
        }
    }
}
