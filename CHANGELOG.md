# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/), y el
versionado, [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### En progreso

- Destinos remotos por FTP (`FtpStorageBackend`, con reanudación e identidad de
  origen): implementado a nivel de motor y modelo de credenciales, pero todavía **no
  expuesto en el asistente de tareas** — no es una funcionalidad usable todavía, solo
  base de la fase 5 del hito 2.
- SFTP y S3, planificación avanzada, acciones pre/post, multiequipo, notificaciones,
  verificación tricolor y empaquetado (fases 5 a 9 del hito 2): sin implementar.

## [1.0.0] - 2026-08-31

Primera versión estable. Cierra el hito 1: el producto completo de un solo equipo,
usable de punta a punta sin ayuda externa.

### Añadido

- Motor de copia local y a recursos de red UNC, incremental (por tamaño y fecha) y en
  modo espejo con papelera de seguridad, con reanudación desde el punto de avance,
  detección de estancamiento por bytes, resiliencia con reintentos y disyuntor por
  destino, y verificación posterior a la copia (tamaño y SHA-256 opcional).
- Simulación (*dry run*), exclusiones y filtros, y seis estrategias de orden de
  ejecución.
- Programación de tareas con frecuencias diaria, semanal, quincenal, mensual y
  personalizada, persistida con Quartz.NET.
- API HTTPS con progreso en vivo por SignalR, autenticación con Argon2id, límite de
  intentos de acceso, cierre de sesión por inactividad, bitácora de accesos, y
  validación estricta de toda ruta recibida contra *path traversal*.
- Certificado autofirmado generado al primer arranque; sin credencial configurada, la
  API solo escucha en `127.0.0.1`.
- Servicio de Windows con arranque automático retrasado, corriendo bajo una cuenta de
  servicio dedicada (no *LocalSystem*) con credenciales de red cifradas por DPAPI.
- Interfaz web completa en React: pantalla principal, asistente de tareas de seis
  pestañas, configuración, historial y logs con exportación, inicio de sesión, tema
  claro/oscuro, y diseño responsive usable desde el celular.
- Aplicación de escritorio en WPF con WebView2, bandeja del sistema, bloqueo por
  contraseña, y detección/arranque automático del servicio.
- Exportación e importación de configuración y tareas en JSON.
- Cobertura de pruebas automatizadas en Domain, Application, Storage e integración de
  API.
