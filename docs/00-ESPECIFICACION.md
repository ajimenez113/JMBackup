# JMBackup — Especificación funcional

Versión 1.0 · Documento base para el desarrollo.
Identificadores: `RF-xx` funcional · `RNF-xx` no funcional.

**Etiquetas de alcance**, en cada requisito:

| Etiqueta | Significado |
|---|---|
| **[H1]** | Hito 1 — fases 0 a 4. **Es lo que se construye ahora.** |
| **[H2]** | Hito 2 — fases 5 a 9. Diseñar para que quepa, **no implementar**. |

Las **[DECISIÓN]** son criterios tomados para llenar vacíos del documento original;
están marcadas para que el autor las confirme o cambie.

---

## 1. Visión

JMBackup es una aplicación de respaldo para Windows que copia archivos y carpetas de
un equipo a otro por red, hacia carpetas compartidas (SMB/UNC) y, más adelante,
hacia servidores FTP/FTPS/SFTP y Amazon S3. Se opera desde una interfaz de escritorio
o desde una interfaz web servida por el propio equipo, y funciona tanto en una
máquina aislada como emparejada con otras instancias.

**No objetivos (nunca):** imágenes de disco / bare metal, agentes específicos para
motores de base de datos, clientes para macOS o Linux, respaldo del estado del
sistema o del registro de Windows.

### Qué entrega el hito 1

Una aplicación de respaldo **funcional y utilizable en producción sobre un equipo**:

- Motor de copia completo: incremental, espejo, exclusiones, filtros, seis órdenes
  de procesamiento, reintentos, reanudación, papelera de seguridad, simulación
- Orígenes y destinos locales y por red SMB/UNC
- Planificación horaria: diaria, semanal, quincenal, mensual y personalizada
- Ejecución como servicio de Windows, en segundo plano, arrancando con el sistema
- Interfaz web completa sobre HTTPS, con autenticación
- Aplicación de escritorio con bandeja del sistema y diálogos nativos
- Historial y logs

### Qué NO entrega el hito 1

FTP, SFTP, S3, emparejamiento entre equipos, acciones pre/post backup, respaldo en
tiempo real, ventana de respaldo, recuperación de ejecuciones perdidas, correo,
comparación tricolor, VSS, instalador con Inno Setup.

---

## 2. Identidad visual · **[H1]**

| Elemento | Valor |
|---|---|
| Color de marca | Azul marino `#000080` — RGB(0, 0, 128) |
| Uso | Bordes, contornos, sombras, foco, acentos y estados activos |
| Temas | Claro y oscuro, conmutables desde Configuración |
| Regla | El azul marino **no cambia** entre temas. Es el identificador de la app |
| Estilo | Minimalista: bordes redondeados, sombras suaves, mucho aire |

**[DECISIÓN]** Sobre fondo oscuro, `#000080` puro no alcanza el contraste mínimo para
texto (falla WCAG AA). Se define: `#000080` como base para bordes y sombras en ambos
temas, y `#4D4DFF` para texto y elementos interactivos sobre fondo oscuro. Se
conserva la identidad y también la accesibilidad.

Tokens (`web/src/theme/tokens.css`):
```
--jm-brand: #000080;
--jm-brand-light: #4D4DFF;
--jm-radius: 12px;
--jm-shadow: 0 2px 12px rgba(0, 0, 128, 0.18);
--jm-shadow-lg: 0 8px 32px rgba(0, 0, 128, 0.24);
```

Semáforo de estado: `verde #16A34A` · `amarillo #EAB308` · `rojo #DC2626`.

---

## 3. Pantalla principal

Barra de acciones global, idéntica en escritorio y web:

| Botón | Icono | Comportamiento | Alcance |
|---|---|---|---|
| Ejecutar todo | ▶▶ | Encola todas las tareas habilitadas | **[H1]** |
| Iniciar | ▶ | Ejecuta la tarea seleccionada; sin selección, la primera | **[H1]** |
| Pausar | ⏸ | Pausa la ejecución en curso (o todas). Reanudable | **[H1]** |
| Cancelar | ■ | Cancela la ejecución en curso (o todas) | **[H1]** |
| Agregar | ➕ | Abre el asistente de nueva tarea | **[H1]** |
| Configuración | ⚙ | Abre Configuración | **[H1]** |
| Acerca de | ⓘ | Versión, licencia, créditos, enlace a la web local | **[H1]** |

**RF-01 [H1]** Lista de tareas con nombre, grupo, estado, última ejecución, resultado,
próxima ejecución y barra de progreso en vivo mientras corre.
**RF-02 [H1]** Agrupación plegable por grupo.
**RF-03 [H1]** El progreso llega por SignalR: archivo actual, bytes
transferidos/totales, velocidad, tiempo restante estimado, conteo de archivos
correctos y fallidos.
**[DECISIÓN] RF-04 [H1]** Panel superior de salud: última copia exitosa por tarea, con
alerta visual si una tarea lleva más de su intervalo esperado sin éxito (indicador de
RPO). Es lo que distingue una herramienta profesional de un copiador de archivos.

---

## 4. Asistente de tarea (botón Agregar)

Diálogo de pestañas, usado tanto para crear como para editar.
En el hito 1 se muestran cinco pestañas: General, Archivos, Horario, Exclusiones,
Filtros y Avanzado. Las pestañas **Acciones** y parte de **Avanzado** llegan en el hito 2.

### 4.1 General

**RF-10 [H1]** Nombre de la tarea (obligatorio, único).
**RF-11 [H1]** Grupo: seleccionar uno existente o crear uno nuevo.
**RF-12 [H1]** Interruptor Habilitada / Deshabilitada.
**RF-13 [H1]** Interruptor "Incluir subcarpetas" (activado por defecto).

**RF-14** Modo de la tarea, selección única:
- **Incremental** — copia solo lo nuevo o modificado · **[H1]**
- **Espejo** — el destino queda idéntico al origen; lo que no está en el origen se
  elimina del destino · **[H1]**
- **Solo enviar** — únicamente sube al destino (equipos emparejados) · **[H2]**
- **Solo recibir** — únicamente baja desde el otro equipo · **[H2]**

> El enum del dominio incluye los cuatro valores desde el hito 1; la interfaz solo
> ofrece los dos primeros y la API rechaza los otros dos con un error claro.

**RF-15 [H1]** Orden de procesamiento, selección única:
- Alfabético ascendente (A→Z)
- Alfabético descendente (Z→A)
- Tamaño: mayor primero
- Tamaño: menor primero
- Fecha de modificación: más antiguo primero
- Fecha de modificación: más reciente primero

**RF-16 [H2]** Modo tiempo real: la tarea queda activa y respalda cada archivo apenas
aparece o cambia, respetando el orden de RF-15 para lo pendiente.
Detalles para cuando llegue: `FileSystemWatcher` con *debounce* de 5 segundos;
verificación de que el archivo no esté en escritura; reescaneo completo ante
desbordamiento del búfer del vigilante; cola persistida en SQLite.

### 4.2 Archivos

**RF-20 [H1]** Bloque **Origen** y bloque **Destino**, cada uno con lista de rutas y
botones agregar / quitar / editar.

**RF-21** Cada ruta se agrega por uno de cuatro métodos:
1. Explorador de archivos (diálogo nativo del sistema) · **[H1]**
2. Ruta manual escrita, incluye UNC `\\SERVIDOR\recurso` · **[H1]**
3. FTP / FTPS / SFTP · **[H2]**
4. Amazon S3 · **[H2]**

> En el hito 1 los métodos 3 y 4 aparecen deshabilitados en la interfaz con la
> leyenda "Próximamente", para que la maqueta ya refleje el diseño final.

**RF-22 [H1]** Cada ruta muestra indicador de conectividad a su izquierda:
- ✅ conectado
- ❌ sin conexión — *tooltip* al pasar el mouse con el motivo exacto
  (credenciales inválidas, host inalcanzable, permiso denegado, ruta inexistente)
- ⏳ verificando

Se verifica al agregar la ruta, luego **[DECISIÓN]** cada 60 segundos mientras el
diálogo esté abierto, y siempre justo antes de ejecutar la tarea.

**RF-23 [H1]** Arrastrar y soltar archivos o carpetas sobre el cuadro de origen los
agrega. En escritorio se resuelve por el puente nativo con la ruta real; en el
navegador **[DECISIÓN]** solo se puede agregar el nombre, porque por seguridad no
expone rutas absolutas. La interfaz lo indica cuando corre en navegador.

**[DECISIÓN] RF-24 [H1]** Verificar espacio disponible en el destino antes de ejecutar
y abortar con mensaje claro si no alcanza.

### 4.3 Horario

**RF-30 [H1]** Frecuencia: diaria · semanal · quincenal · mensual · personalizada.
**RF-31 [H1]** En personalizada: días de la semana y/o días del mes.
**RF-32 [H1]** Una o varias horas de ejecución por día.

**RF-33 [H2]** Ventana de respaldo, en una de dos formas: duración máxima en minutos,
u hora límite de finalización. Al agotarse, la tarea se detiene de forma ordenada y
guarda su punto de avance para retomar en la siguiente ejecución.
**[DECISIÓN] RF-34 [H2]** Recuperación de ejecuciones perdidas: si el equipo estaba
apagado a la hora programada, al encender se ofrece ejecutar. Configurable: ejecutar
siempre / preguntar / ignorar.
**[DECISIÓN] RF-35 [H2]** Opción "Despertar el equipo destino por Wake-on-LAN antes
de iniciar", para tareas hacia equipos emparejados.

### 4.4 Exclusiones · **[H1]** completa

**RF-40 [H1]** Por extensión: casillas con los formatos comunes agrupados (video,
imagen, audio, ofimática, comprimidos, ejecutables, temporales) **más** un campo para
escribir extensiones manualmente.
**RF-41 [H1]** Por nombre de archivo, escrito o seleccionado del explorador.
**RF-42 [H1]** Por carpeta, escrita o seleccionada del explorador.
**RF-43 [H1]** Por coincidencia de texto: excluir todo archivo o carpeta cuyo nombre
contenga determinados caracteres, letras o palabras.
**RF-44 [H1]** Comodines `*` y `?`, y **[DECISIÓN]** expresiones regulares opcionales
con una casilla "usar regex". Las regex se compilan con tiempo límite de 100 ms para
evitar ReDoS.
**RF-45 [H1]** Casilla "distinguir mayúsculas y minúsculas" (desactivada por defecto).
**[DECISIÓN] RF-46 [H1]** Exclusión por tamaño (mayor que / menor que) y por antigüedad.

### 4.5 Filtros (inclusiones) · **[H1]** completa

**RF-50 [H1]** Lo inverso a exclusiones: respaldar obligatoriamente los archivos o
carpetas cuyo nombre contenga las palabras, caracteres o letras indicadas.
**RF-51 [H1]** Casilla por regla: **Prioritario**. Si está marcada, esos elementos se
copian primero, antes que el orden general de RF-15. Si no lo está, se respaldan junto
con el resto pero el motor garantiza que se incluyan.
**RF-52 [H1]** Precedencia: **los filtros ganan sobre las exclusiones**. Un archivo
excluido por regla pero capturado por un filtro se respalda. La interfaz lo muestra
explícitamente para que no sorprenda.

### 4.6 Acciones · **[H2]** completa

Dos listas reordenables por arrastre: **Pre-backup** y **Post-backup**. Se pueden
agregar varias acciones y repetir el mismo tipo.

Disponibles en **ambas** listas:

| Acción | Parámetros |
|---|---|
| Pausar | Segundos |
| Ejecutar | Archivo o aplicación (selector nativo) + argumentos |
| Línea de comandos | Texto del comando + directorio de trabajo |
| Cerrar un programa | Nombre del proceso o ruta del ejecutable |
| Iniciar un servicio | Selector con los servicios de Windows |
| Detener un servicio | Selector con los servicios de Windows |

Exclusivas de **Post-backup**:

| Acción | Parámetros |
|---|---|
| Iniciar una tarea | Ejecutable (selector nativo) |
| Suspender el equipo | — |
| Hibernar el equipo | — |
| Reiniciar el equipo | Retardo en segundos |
| Apagar el equipo | Retardo en segundos |

**RF-60 [H2]** Casilla en Pre-backup: **"Cancelar tarea si una acción falla"**.
**RF-61 [H2]** Casilla en Post-backup: **"No ejecutar si las acciones del pre-backup
fallaron"**.
**RF-62 [H2]** Tiempo máximo de espera por acción, **[DECISIÓN]** 300 s por defecto.
**[DECISIÓN] RF-63 [H2]** Las acciones de apagado, reinicio, suspensión e hibernación
muestran un aviso de 60 segundos cancelable en el equipo.

> **Nota de seguridad para cuando se implemente:** las acciones de línea de comandos
> se ejecutan **sin shell**, con `ProcessStartInfo.ArgumentList`, nunca concatenando
> una cadena. Es un vector de ejecución remota de código.

### 4.7 Avanzado

**RF-70 [H1]** Casilla **Tarea espejo** — replica exacto el origen; elimina en el
destino lo que no exista en el origen. Advertencia destacada al marcarla.
**RF-71 [H1]** Casilla **Usar rutas absolutas** — replica el árbol completo del origen
dentro del destino.
**RF-72 [H1]** Casilla **Eliminar directorios vacíos** en el destino.

**[DECISIÓN] RF-73 [H1] — Papelera de seguridad.** En modo espejo lo "eliminado" no se
borra: se mueve a `_JMBackup_Papelera\AAAA-MM-DD\` en el destino, con retención
configurable (30 días por defecto). Un espejo mal configurado que borra datos reales
es el peor fallo posible en software de respaldo; esto lo hace reversible.
**[DECISIÓN] RF-74 [H1] — Simulación (*dry run*).** Ejecutar la tarea sin escribir
nada, mostrando qué se copiaría, qué se omitiría y qué se eliminaría. **Obligatorio
antes de la primera ejecución de una tarea en modo espejo.**
**[DECISIÓN] RF-75 [H1]** Verificación posterior a la copia: comparar tamaño y,
opcionalmente, hash SHA-256 de cada archivo copiado.
**[DECISIÓN] RF-76 [H2]** Compresión opcional en tránsito y cifrado opcional AES-256
en destino, útil sobre todo para S3.
**[DECISIÓN] RF-77 [H2]** Retención de versiones estilo GFS (diarias / semanales /
mensuales) para destinos que no sean espejo.

---

## 5. Configuración de la aplicación

### 5.1 Correo · **[H2]** completa

**RF-80 [H2]** SMTP: servidor, puerto, cifrado, usuario, contraseña, remitente.
**RF-81 [H2]** Asunto personalizable con variables `{tarea}`, `{equipo}`,
`{resultado}`, `{fecha}`.
**RF-82 [H2]** Cuerpo introductorio personalizable.
**RF-83 [H2]** Lista de destinatarios (varios).
**RF-84 [H2]** Botón "Enviar correo de prueba".
**RF-85 [H2]** Estructura fija del correo, breve: nombre de la tarea, equipo destino,
hora de inicio y fin, duración, resultado (completado / con errores / fallido), conteo
de archivos copiados, omitidos y con error **sin listarlos**, y enlace a la interfaz
web del equipo.
**[DECISIÓN] RF-86 [H2]** Selector de cuándo enviar: siempre / solo si hay errores /
solo si falla / nunca. Por defecto, **solo si hay errores**: recibir cuarenta correos
verdes al día hace que nadie lea el rojo.
**[DECISIÓN] RF-87 [H2]** Notificaciones alternativas: *toast* de Windows y *webhook*
genérico (compatible con Telegram, Slack, Teams).

### 5.2 Transferencia

**RF-90 [H2]** Casilla "Limitar velocidad de transferencia" que habilita un campo de
velocidad. **[NOTA]** El documento original dice "velocidad en segundos"; se
interpreta como **KB/s o MB/s** con unidad seleccionable. Confirmar.
**RF-91 [H2]** Casilla "Reintentar transferencias fallidas" (específica de FTP).
**RF-92 [H2]** Lista de extensiones que se transfieren en modo ASCII.

**[H1]** — opciones de esta pestaña que sí entran en el hito 1, porque aplican a
copias locales y SMB:
- Número de transferencias en paralelo (1–16, por defecto 4)
- Límite global de ancho de banda (útil para no saturar el enlace copiando a un NAS)
- Tamaño de bloque de lectura/escritura
- Preservar marcas de tiempo y atributos

**[H2]** — resto: tiempo de espera de conexión y lectura · modo pasivo/activo FTP ·
reanudación de parciales (`REST`, *multipart*) · límite de ancho de banda por horario ·
verificación de certificado TLS del servidor FTPS · para S3, clase de almacenamiento,
cifrado del lado del servidor, tamaño de parte del *multipart* y aviso de costo al
elegir Glacier.

### 5.3 Seguridad y acceso · **[H1]** completa

**RF-100 [H1]** Definir usuario y contraseña.
**RF-101 [H1]** Elegir dónde se exige la credencial: solo web · solo aplicación ·
ambas · ninguna.
**RF-102 [H1]** Si se elige "ninguna", advertencia explícita del riesgo.
**RF-103 [H1]** Política: mínimo 8 caracteres, al menos una mayúscula, una minúscula y
un número. Sin límite superior. Caracteres especiales permitidos.
**RF-104 [H1]** Si la aplicación de escritorio tiene contraseña, se pide al abrir la
ventana o al intentar modificar algo, no para verla en la bandeja.
**[DECISIÓN] RF-105 [H1] — Excepción de seguridad.** Sin credencial configurada, el
servidor escucha **solo en 127.0.0.1** y no es accesible desde la red. Exponerlo a la
LAN exige credencial. Para exponerlo sin credencial hay que confirmarlo escribiendo
una frase.
**[DECISIÓN] RF-106 [H1]** Cierre de sesión web por inactividad (30 min por defecto).
**[DECISIÓN] RF-107 [H1]** Bitácora de accesos: quién entró, desde qué IP, cuándo, y
los intentos fallidos.

### 5.4 Interfaz web · **[H1]** completa

**RF-110 [H1]** El servicio publica la interfaz web sobre **HTTPS**.
**RF-111 [H1]** URL por defecto `https://<IP-del-equipo>:8483`. Puerto y dirección de
escucha configurables. Si el puerto está ocupado, se avisa y se sugiere el siguiente libre.
**RF-112 [H1]** La URL configurada es la que se usará en las notificaciones del hito 2.
**RF-113 [H1]** Certificado autofirmado generado al primer arranque, con opción de
instalarlo en el almacén de confianza de Windows o importar uno propio.

### 5.5 General

**RF-120 [H1]** Tema claro / oscuro / seguir al sistema.
**RF-121 [H1]** Iniciar con Windows y ejecutarse en segundo plano (servicio).
**RF-122 [H2]** Idioma: español / inglés. En el hito 1 todo en español, pero **sin
cadenas incrustadas en los componentes**: todas en un archivo de recursos, para que
la internacionalización después sea sustituir el archivo y no reescribir la interfaz.
**RF-123 [H1]** Exportar e importar la configuración completa y las tareas en JSON.

---

## 6. Registro e historial

**RF-130 [H1] Historial** — por cada ejecución: nombre de la tarea, fecha y hora de
inicio, fecha y hora de fin, duración, y si terminó o no. Nada más. Filtrable por
tarea y por rango de fechas.

**RF-131 [H1] Logs** — dos pestañas:
- **Respaldados**: todas las carpetas y archivos que sí se copiaron
- **Errores**: los que fallaron, con motivo, número de reintentos y marca de tiempo

**RF-132 [H1]** Exportar historial y logs a CSV.
**RF-133 [H1]** Retención configurable **[DECISIÓN]** 90 días por defecto, con purga
automática.
**[DECISIÓN] RF-134 [H2]** Envío opcional de eventos por **syslog en formato CEF**,
para integración con un SIEM (WAZUH u otro).
**[DECISIÓN] RF-135 [H2]** Endpoint `/metrics` en formato Prometheus y endpoint REST
simple con el estado de cada tarea, para monitoreo desde PRTG.

---

## 7. Comparación y verificación · **[H2]** completa

**RF-140 [H2] Caso A — ambos equipos tienen JMBackup y están emparejados.**
El equipo que recibe realiza el análisis y compara su contenido con el inventario que
le envía el equipo origen. Árbol con semáforo:
- 🟢 respaldado correctamente (existe y coincide)
- 🟡 respaldado parcialmente (existe pero difiere en tamaño)
- 🔴 no respaldado (no existe o falló)

**RF-141 [H2] Caso B — el equipo destino no tiene JMBackup.** El análisis lo hace el
equipo origen leyendo el destino, al terminar la tarea.
**RF-142 [H2]** Si la tarea tiene una acción post de apagado, suspensión o
hibernación, la verificación se **aplaza**: queda encolada y se ejecuta en segundo
plano al siguiente arranque, con prioridad baja de CPU y de E/S.
**[DECISIÓN] [H2]** Nivel de comparación por tarea: rápida (existencia + tamaño),
media (+ fecha), profunda (hash SHA-256).
**[DECISIÓN] RF-143 [H2]** Verificación programada independiente (*scrub*). Un
respaldo que nunca se verifica no es un respaldo.

> El hito 1 sí incluye la verificación básica posterior a la copia (RF-75), que es la
> pieza sobre la que se construye todo esto.

---

## 8. Multiequipo y emparejamiento · **[H2]** completa

**RF-150 [H2]** La aplicación funciona de forma autónoma en un solo equipo.
*(Esto ya se cumple en el hito 1: es el estado por defecto.)*
**RF-151 [H2]** Descubrimiento automático de otras instancias en la LAN por mDNS.
**RF-152 [H2]** Emparejamiento por tres vías: invitación, ID copiado manualmente, o
código QR.
**RF-153 [H2]** El emparejamiento es **persistente**. Una vez emparejados, las tareas
solo seleccionan el equipo de una lista.
**RF-154 [H2]** Explorador remoto para elegir la carpeta de destino en el otro equipo.
**RF-155 [H2]** El emparejamiento requiere aprobación explícita en **ambos** equipos.
**[DECISIÓN] Modelo de confianza [H2].** Cada instalación genera un par de claves. El
emparejamiento intercambia claves públicas mediante un token de un solo uso con
vencimiento de 10 minutos (transportado en el ID o el QR). Después, todo el tráfico
entre pares usa **mTLS**. Un ID interceptado no sirve para nada.
**[DECISIÓN] RF-156 [H2]** Panel de equipos emparejados: estado en línea, última vez
visto, revocar.

---

## 9. Motor de copia: reglas de comportamiento

**RF-160 [H1] Manejo de errores.** Si un archivo o carpeta falla, se **omite** y el
respaldo continúa. Al terminar el resto, se reintentan los omitidos, **máximo 3 veces**.

**RF-161 [H1] Detección de estancamiento.** Si durante **3 minutos** los bytes
transferidos de un archivo no avanzan, se cancela ese archivo. Si avanza, aunque sea
lento, se deja continuar. La medición es **por bytes, nunca por tiempo total**: un
archivo de 40 GB puede tardar horas legítimamente.

**RF-162 [H1] Reintentos con espera creciente** entre intentos (**[DECISIÓN]** 5 s,
30 s, 120 s).

**RF-163 [H1] Reanudación.** Si se cancela, la tarea guarda su punto de avance y
retoma sin recopiar lo ya copiado.

**RF-164 [H1] Detección de cambios (incremental).** Comparación rápida por tamaño +
fecha de modificación. El hash SHA-256 solo se calcula con verificación profunda
activada, y se cachea en SQLite.

**RF-165 [H1] Escritura atómica.** Los archivos se copian a un temporal `.jmtmp` y se
renombran al completarse. Nunca queda un archivo truncado haciéndose pasar por bueno.

**[DECISIÓN] RF-166 [H2] Archivos bloqueados.** Los archivos abiertos por otro proceso
no se pueden copiar en Windows. Se resuelve con **VSS**. En el hito 1 se registran
como error explícito y comprensible, no como fallo genérico.

**[DECISIÓN] RF-167 [H1] Rutas largas.** Soporte de rutas de más de 260 caracteres:
manifiesto con `longPathAware` y activación de `LongPathsEnabled`.

---

## 10. Requisitos no funcionales

| ID | Requisito | Alcance |
|---|---|---|
| RNF-01 | Windows 10 (1809+) y Windows 11, x64 | **[H1]** |
| RNF-02 | El servicio corre en segundo plano y arranca con el sistema | **[H1]** |
| RNF-03 | Consumo en reposo < 100 MB de RAM y < 1 % de CPU | **[H1]** |
| RNF-04 | La interfaz nunca se congela: toda operación pesada es asíncrona | **[H1]** |
| RNF-05 | Reinicio del servicio sin pérdida de estado ni de la cola pendiente | **[H1]** |
| RNF-06 | Instalador con firma de código | **[H2]** |
| RNF-07 | Cobertura de pruebas ≥ 80 % en Domain, Application y Storage | **[H1]** |
| RNF-08 | Interfaz web usable en móvil (responsive) | **[H1]** |
| RNF-09 | Documentación en español, código en inglés | **[H1]** |
| RNF-10 | Arranque de la interfaz < 3 segundos | **[H1]** |
| RNF-11 | Publicación autocontenida: no exige instalar el runtime de .NET aparte | **[H1]** |
| RNF-12 | Instalador con Inno Setup | **[H2]** |

---

## 11. Puntos abiertos que requieren confirmación del autor

**Afectan al hito 1 — decidir pronto:**

1. **¿El servicio corre como cuenta de servicio dedicada o como *LocalSystem*?**
   El ADR-008 recomienda cuenta dedicada, porque *LocalSystem* no ve unidades
   mapeadas y se autentica en la red como la cuenta de máquina, lo que rompe el
   acceso a recursos SMB. Confirmar.
2. **¿El destino habitual son equipos Windows, o también NAS y servidores Linux por
   SMB?** Cambia las suposiciones sobre permisos, sobre distinción de mayúsculas en
   nombres de archivo, y sobre la precisión de las marcas de tiempo.
3. ¿Se requiere multiusuario (varias cuentas con permisos distintos) o basta una?

**Afectan solo al hito 2 — decidir después:**

4. Unidad del límite de velocidad en FTP (RF-90): ¿KB/s o MB/s?
5. ¿Cuántos equipos emparejados como máximo se prevén?
6. ¿Habrá certificado de firma de código para el instalador? Sin él, SmartScreen
   mostrará advertencia en cada instalación.
