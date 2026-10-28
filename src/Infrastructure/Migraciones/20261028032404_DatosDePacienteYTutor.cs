using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TherapEase.Infrastructure.Migraciones
{
    // Escrita a mano sobre lo que generó EF Core: EF habría interpretado el cambio como renombrar Contacto a TutorNombre, que descartaría la correspondencia aprobada.
    //
    // SUBIDA (todo en la transacción de la migración; si algo falla, no queda nada a medias y Contacto sigue en su sitio):
    //   1. Agrega las columnas nuevas. NombreNormalizado entra anulable y se vuelve obligatoria después de rellenarla.
    //   2. Cancela con un mensaje claro si algún nombre no produce una clave de búsqueda válida (vacía o de más de 600 caracteres), antes de cambiar datos.
    //   3. Rellena NombreNormalizado con la misma clave que Domain (NormalizacionDeNombres).
    //   4. Copia Contacto a Correo y Telefono según la correspondencia aprobada por Lucía el 26-oct-2026 (tabla en la propuesta B02, sección 3.1).
    //   5. Retira Contacto y agrega índice y restricciones.
    //   Pérdida admitida solo en datos de prueba: el texto de Contacto que no es un correo ni un teléfono válidos, los espacios exteriores, las mayúsculas del correo y
    //   los separadores del teléfono. No se inventan nacimiento, escuela, grupo ni tutor: quedan nulos.
    //
    // BAJADA:
    //   Recrea Contacto (varchar(200)) con el correo si existe y mide hasta 200; si no, con el teléfono; si no, nulo. Un correo de más de 200 caracteres no cabe en la
    //   columna anterior y no se trunca (un correo cortado sería otro dato): se usa el teléfono, o nulo. Si había correo y teléfono, se pierde el teléfono. Se
    //   pierden también nacimiento, escuela, grupo, tutor y nombre normalizado al retirar sus columnas; ninguna se puede reconstruir desde Contacto.
    //
    // Correspondencia en SQL frente a Domain: la clave de nombre usa normalize(..., NFD), regexp_replace y translate de PostgreSQL; la validación de correo y
    // teléfono replica ReglasDeNombreYCorreo y ReglasDeTelefono con los caracteres de formato del plano básico de Unicode. Las pruebas de integración comparan ambas
    // con las mismas muestras. No usa el reloj ni la configuración regional del servidor para decidir mayúsculas de la clave; el correo sí usa lower() de PostgreSQL.
    /// <inheritdoc />
    public partial class DatosDePacienteYTutor : Migration
    {
        // Espacios que recorta string.Trim() en .NET, además de los de la clase [:space:] de PostgreSQL (que según la configuración regional puede omitir los no ASCII).
        private const string Espacios = "[[:space:]\\u0085\\u00a0\\u1680\\u2000-\\u200a\\u2028\\u2029\\u202f\\u205f\\u3000]";

        // Control (Cc) y formato (Cf) del plano básico, más los separadores de línea y de párrafo (Zl, Zp): lo que ReglasDeNombreYCorreo.EsCaracterInvisible rechaza.
        private const string Prohibidos =
            "[[:cntrl:]\\u00ad\\u0600-\\u0605\\u061c\\u06dd\\u070f\\u0890\\u0891\\u08e2\\u180e\\u200b-\\u200f\\u2028-\\u202e\\u2060-\\u2064\\u2066-\\u206f\\ufeff\\ufff9-\\ufffb]";

        // Clave de nombre: forma D, sin marcas combinantes U+0300-U+036F, A-Z a minúscula y espacios colapsados y recortados.
        private const string ClaveDeNombre =
            "btrim(regexp_replace(" +
                "translate(regexp_replace(normalize(\"Nombre\", NFD), '[\\u0300-\\u036f]', '', 'g'), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), " +
                "'[ \\t\\n\\r\\v\\f]+', ' ', 'g'), ' ')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Paciente",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Escuela",
                table: "Paciente",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "Paciente",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grupo",
                table: "Paciente",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreNormalizado",
                table: "Paciente",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Paciente",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorCorreo",
                table: "Paciente",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorNombre",
                table: "Paciente",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorParentesco",
                table: "Paciente",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorTelefono",
                table: "Paciente",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Antes de cambiar datos: si algún nombre no produce una clave válida, se cancela todo con un mensaje que no incluye el nombre.
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM \"Paciente\" WHERE length(" + ClaveDeNombre + ") NOT BETWEEN 1 AND 600) THEN " +
                "RAISE EXCEPTION 'Hay fichas cuyo nombre no produce una clave de busqueda valida; la migracion se cancela sin cambios.'; " +
                "END IF; END $$;");

            migrationBuilder.Sql("UPDATE \"Paciente\" SET \"NombreNormalizado\" = " + ClaveDeNombre + ";");

            migrationBuilder.AlterColumn<string>(
                name: "NombreNormalizado",
                table: "Paciente",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(600)",
                oldMaxLength: 600,
                oldNullable: true);

            // Contacto -> Correo y Telefono. Un correo válido (minúsculas, sin espacios exteriores) va a Correo; un teléfono válido (dígitos y + inicial) va a Telefono;
            // cualquier otro texto, incluidas las mezclas, se descarta. Nunca se quitan letras para forzar un teléfono.
            migrationBuilder.Sql(
                "WITH base AS (" +
                    "SELECT \"IdPaciente\", regexp_replace(\"Contacto\", '^" + Espacios + "+|" + Espacios + "+$', '', 'g') AS t " +
                    "FROM \"Paciente\" WHERE \"Contacto\" IS NOT NULL), " +
                "conv AS (" +
                    "SELECT \"IdPaciente\", t, lower(t) AS c, split_part(lower(t), '@', 2) AS d, regexp_replace(t, '[^0-9]', '', 'g') AS digitos FROM base) " +
                "UPDATE \"Paciente\" p SET " +
                    "\"Correo\" = CASE WHEN length(conv.c) BETWEEN 1 AND 254 " +
                        "AND conv.c !~ '" + Espacios + "' AND conv.c !~ '" + Prohibidos + "' " +
                        "AND conv.c ~ '^[^@]+@[^@]+$' " +
                        "AND conv.d LIKE '%.%' AND conv.d NOT LIKE '.%' AND conv.d NOT LIKE '%.' AND conv.d NOT LIKE '%..%' " +
                        "THEN conv.c END, " +
                    "\"Telefono\" = CASE WHEN conv.t ~ '^\\+?[0-9 .()-]+$' AND length(conv.digitos) BETWEEN 7 AND 15 " +
                        "THEN (CASE WHEN left(conv.t, 1) = '+' THEN '+' ELSE '' END) || conv.digitos END " +
                "FROM conv WHERE p.\"IdPaciente\" = conv.\"IdPaciente\";");

            migrationBuilder.DropColumn(
                name: "Contacto",
                table: "Paciente");

            migrationBuilder.CreateIndex(
                name: "IX_Paciente_NombreNormalizado",
                table: "Paciente",
                column: "NombreNormalizado");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Paciente_FechaNacimientoMinima",
                table: "Paciente",
                sql: "\"FechaNacimiento\" IS NULL OR \"FechaNacimiento\" >= DATE '1900-01-01'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Paciente_NombreNormalizadoFormato",
                table: "Paciente",
                sql: "length(\"NombreNormalizado\") > 0 AND \"NombreNormalizado\" !~ '[A-Z\\t\\n\\r\\v\\f\\u0300-\\u036f]' AND \"NombreNormalizado\" !~ '^ | $|  '");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Paciente_TutorCompleto",
                table: "Paciente",
                sql: "(\"TutorNombre\" IS NULL AND \"TutorParentesco\" IS NULL AND \"TutorTelefono\" IS NULL AND \"TutorCorreo\" IS NULL) OR (\"TutorNombre\" IS NOT NULL AND \"TutorParentesco\" IS NOT NULL AND (\"TutorTelefono\" IS NOT NULL OR \"TutorCorreo\" IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contacto",
                table: "Paciente",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Correo si cabe en 200 caracteres; si no, teléfono; si no, nulo. Nunca se trunca un dato.
            migrationBuilder.Sql(
                "UPDATE \"Paciente\" SET \"Contacto\" = " +
                "CASE WHEN \"Correo\" IS NOT NULL AND length(\"Correo\") <= 200 THEN \"Correo\" ELSE \"Telefono\" END;");

            migrationBuilder.DropIndex(
                name: "IX_Paciente_NombreNormalizado",
                table: "Paciente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Paciente_FechaNacimientoMinima",
                table: "Paciente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Paciente_NombreNormalizadoFormato",
                table: "Paciente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Paciente_TutorCompleto",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "Escuela",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "Grupo",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "NombreNormalizado",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "TutorCorreo",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "TutorNombre",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "TutorParentesco",
                table: "Paciente");

            migrationBuilder.DropColumn(
                name: "TutorTelefono",
                table: "Paciente");
        }
    }
}
