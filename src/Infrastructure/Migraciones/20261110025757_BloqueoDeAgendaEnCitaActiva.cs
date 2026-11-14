using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TherapEase.Infrastructure.Migraciones
{
    /// <inheritdoc />
    /// <remarks>
    /// Serializa las altas y los cambios de citas activas con un bloqueo de asesoría de transacción, tomado dentro de un disparador de Cita.
    /// Sin cambios del modelo ni de datos: solo una función y dos disparadores. La restricción de cruces y los controles del paciente no se tocan.
    ///
    /// Por qué: EX_Cita_SinCruceActivo es una restricción de exclusión; cada alta inserta su fila y después comprueba si choca. Con dos altas conflictivas
    /// que llegan a la vez, cada una encuentra la fila aún sin confirmar de la otra y espera su transacción, de modo que PostgreSQL aborta una por
    /// interbloqueo (40P01) en lugar de rechazarla con el cruce (23P01). Con el bloqueo, la segunda espera a que la primera confirme o se revierta y la
    /// restricción compara contra filas ya confirmadas: el resultado es siempre un cruce real o un guardado.
    ///
    /// Orden: PostgreSQL ejecuta los disparadores BEFORE del mismo evento por orden alfabético de su nombre (comparación byte a byte). Los nuevos empiezan
    /// con «00» para disparar antes de TR_Cita_ActivaExigePacienteVigente: primero el bloqueo de asesoría y después el FOR SHARE del paciente. Ninguna otra
    /// ruta toma el bloqueo del paciente y después escribe una cita activa (la baja bloquea al paciente y solo lee Cita), así que no hay ciclo.
    ///
    /// Reversión: Down elimina los dos disparadores y la función. No quedan datos que revertir.
    /// </remarks>
    public partial class BloqueoDeAgendaEnCitaActiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clave fija 7301: única para toda la agenda (hoy hay una sola agenda). pg_advisory_xact_lock se libera sola al confirmar o revertir.
            migrationBuilder.Sql("""
                CREATE FUNCTION "FnCitaSerializaCruceActivo"() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(7301);
                    RETURN NEW;
                END $$;
                """);

            // Solo cuando la fila resultante queda agendada y vigente: al insertarla y al cambiar inicio, fin, estado o condición.
            // Una cita que se cancela o se da de baja libera horario y no necesita el bloqueo; cambiar solo el pago o los metadatos tampoco.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Cita_00SerializaAltaActiva"
                BEFORE INSERT ON "Cita"
                FOR EACH ROW WHEN (NEW."Estado" = 'Agendada' AND NEW."Condicion" = 'Vigente')
                EXECUTE FUNCTION "FnCitaSerializaCruceActivo"();
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Cita_00SerializaCambioActivo"
                BEFORE UPDATE OF "Inicio", "Fin", "Estado", "Condicion" ON "Cita"
                FOR EACH ROW WHEN (
                    NEW."Estado" = 'Agendada' AND NEW."Condicion" = 'Vigente'
                    AND (NEW."Inicio", NEW."Fin", NEW."Estado", NEW."Condicion")
                        IS DISTINCT FROM (OLD."Inicio", OLD."Fin", OLD."Estado", OLD."Condicion"))
                EXECUTE FUNCTION "FnCitaSerializaCruceActivo"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_Cita_00SerializaCambioActivo" ON "Cita";
                DROP TRIGGER IF EXISTS "TR_Cita_00SerializaAltaActiva" ON "Cita";
                DROP FUNCTION IF EXISTS "FnCitaSerializaCruceActivo"();
                """);
        }
    }
}
