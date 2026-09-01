# Primera tarea

## Abrir JMBackup

Con el servicio instalado y corriendo (ver
[`../04-GUIA-INSTALACION.md`](../04-GUIA-INSTALACION.md)), abrí la aplicación de
escritorio desde el acceso directo. Detecta sola si el servicio ya está corriendo y,
si no, lo arranca. La ventana muestra la misma interfaz que verías entrando por
navegador a `https://<equipo>:8483` — es la misma aplicación, dos formas de abrirla.

`[CAPTURA: ventana principal recién abierta, lista de tareas vacía]`

Si es la primera vez que abrís la aplicación y configuraste una contraseña de acceso,
vas a ver la pantalla de inicio de sesión antes de la lista de tareas.

## La pantalla principal

Arriba, la barra de acciones:

- **Ejecutar todo** — encola todas las tareas habilitadas.
- **Iniciar / Pausar / Cancelar** — actúan sobre la tarea seleccionada, o sobre todas
  si no hay ninguna seleccionada.
- **Agregar** — abre el asistente de nueva tarea (ver
  [`02-asistente-de-tareas.md`](02-asistente-de-tareas.md)).
- **Configuración** — ajustes generales, de transferencia, seguridad y de la interfaz
  web.
- **Acerca de** — versión instalada, y el enlace a la interfaz web local.

Debajo, la lista de tareas, agrupada y plegable, con nombre, grupo, estado (Corriendo,
Al día, Con errores, Falló, Cancelada, o Deshabilitada), última ejecución, resultado y
próxima ejecución programada.

`[CAPTURA: barra de acciones con cada botón señalado]`

## Crear la primera tarea

1. Hacé clic en **Agregar**. Se abre el asistente, en la pestaña **General**.
2. Ponele un nombre. Grupo es opcional — sirve para plegar varias tareas relacionadas
   en la lista.
3. Elegí el **Modo**:
   - **Incremental** — copia solo lo nuevo o modificado desde la última vez. La
     opción normal para la mayoría de los respaldos.
   - **Espejo** — el destino queda idéntico al origen: lo que borrás en el origen
     también desaparece del destino (a una papelera de seguridad local,
     `_JMBackup_Papelera\`, no un borrado directo). Usalo con cuidado la primera vez —
     ver el aviso de la pestaña Avanzado.
4. Andá a la pestaña **Archivos** y agregá al menos una ruta de origen y una de
   destino — ver el detalle de esta pestaña en
   [`02-asistente-de-tareas.md`](02-asistente-de-tareas.md).
5. (Opcional pero recomendado la primera vez) Andá a la pestaña **Avanzado** y hacé
   clic en **Simular** — corre una simulación completa sin copiar ni borrar nada de
   verdad, y te muestra qué se copiaría, qué se omitiría y (en modo espejo) qué se
   eliminaría.
6. Guardá. Vas a volver a la lista de tareas, con la nueva tarea ya creada.

`[CAPTURA: asistente en la pestaña General, con un nombre y modo Incremental elegidos]`

## Ejecutarla

Seleccioná la tarea en la lista y hacé clic en **Iniciar** (o **Ejecutar todo** si es
la única). El progreso se actualiza en vivo: archivo actual, velocidad, tiempo
restante. Al terminar, el resultado y la hora quedan en la columna correspondiente, y
el detalle completo en **Historial**.

`[CAPTURA: tarea corriendo, con la barra de progreso y velocidad visibles]`

## Programarla para que corra sola

Sin un horario configurado (pestaña **Horario** del asistente), una tarea solo corre
cuando la ejecutás a mano. Para que corra sola, agregá al menos una frecuencia y hora
— diaria, semanal, quincenal, mensual o personalizada.
