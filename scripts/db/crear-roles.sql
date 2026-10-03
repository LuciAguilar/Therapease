-- Crea therapease_migrador (dueño del esquema, solo migraciones) y therapease_app (la aplicación, sin permisos de esquema).
-- Se ejecuta una vez como superusuario; uso y contraseñas por variables de psql en README, sección 10.

CREATE ROLE therapease_migrador LOGIN PASSWORD :'clave_migrador' NOSUPERUSER NOCREATEDB NOCREATEROLE;
CREATE ROLE therapease_app LOGIN PASSWORD :'clave_app' NOSUPERUSER NOCREATEDB NOCREATEROLE;

ALTER DATABASE :"base" OWNER TO therapease_migrador;
REVOKE ALL ON DATABASE :"base" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"base" TO therapease_migrador, therapease_app;
