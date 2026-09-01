# Asistente de tareas — pestaña por pestaña

El asistente tiene seis pestañas. Se accede a él con **Agregar** (tarea nueva) o
haciendo doble clic en una tarea existente (editar).

## General

`[CAPTURA: pestaña General]`

| Campo | Qué hace |
|---|---|
| Nombre | Obligatorio. Identifica la tarea en toda la interfaz. |
| Grupo | Opcional. Agrupa tareas relacionadas, plegables en la lista principal. |
| Habilitada | Una tarea deshabilitada no corre ni sola ni con "Ejecutar todo" (sí se puede seguir iniciando a mano). |
| Incluir subcarpetas | Si está apagado, solo se procesan los archivos directamente dentro de la carpeta de origen. |
| Modo | **Incremental** (copia lo nuevo/modificado) o **Espejo** (el destino queda idéntico al origen). |
| Orden de procesamiento | Alfabético (A→Z o Z→A), por tamaño (mayor o menor primero), o por fecha de modificación (más antiguo o más reciente primero). Afecta el orden en que se copian los archivos, útil por ejemplo para priorizar los más chicos si el respaldo se puede interrumpir. |

## Archivos

`[CAPTURA: pestaña Archivos, con una ruta de origen y una de destino agregadas]`

Acá se agregan las rutas de origen y de destino. Formas de agregar una ruta:

- **Explorador** → **Carpeta** o **Archivo**: abre el selector nativo de Windows (solo
  disponible en la aplicación de escritorio; desde el navegador, arrastrar y soltar
  solo trae el nombre, no la ruta completa — hay que completarla a mano).
- **Ruta manual**: para rutas locales (`C:\...`) o de red (`\\SERVIDOR\recurso`).

Si la ruta es un recurso de red, aparece el selector de **Credencial** — elegí una ya
guardada o creá una nueva ahí mismo (alias para identificarla, usuario en formato
`DOMINIO\usuario`, contraseña). La credencial se guarda cifrada y la usa el servicio
para conectar el recurso antes de cada copia (ver ADR-008).

Cada ruta muestra su estado de conectividad (Verificando… / Conectado / Sin conexión)
con el motivo exacto si falla: archivo bloqueado, host inalcanzable, permiso denegado,
ruta inexistente, o credenciales inválidas.

> Las opciones de FTP/FTPS/SFTP y Amazon S3 todavía dicen "Próximamente": el motor ya
> tiene la primera de las tres construida (FTP), pero ninguna está conectada todavía
> al asistente — quedan para cuando termine esa parte de la fase 5.

## Horario

`[CAPTURA: pestaña Horario, con una frecuencia semanal configurada]`

Sin ningún horario agregado, la tarea solo corre a mano. **Agregar horario** permite
elegir la frecuencia (diaria, semanal, quincenal, mensual, o personalizada), y para
las que aplican, los días de la semana o del mes, y una o más horas de ejecución por
horario.

## Exclusiones

`[CAPTURA: pestaña Exclusiones]`

Reglas que sacan archivos del respaldo, por: extensión (con grupos de formatos
comunes ya armados, o una lista manual separada por coma), nombre de archivo, carpeta,
coincidencia de texto (con opción de usar expresiones regulares y distinguir
mayúsculas), tamaño (mayor/menor que N bytes), o antigüedad (más de N días).

## Filtros

`[CAPTURA: pestaña Filtros]`

Un filtro es lo opuesto a una exclusión: **fuerza** la inclusión de un patrón aunque
una exclusión lo hubiera sacado. Los filtros ganan sobre las exclusiones — un archivo
excluido pero capturado por un filtro se respalda igual. Útil para, por ejemplo,
excluir todos los `.tmp` en general pero incluir igual un `config.tmp` puntual que sí
importa.

## Avanzado

`[CAPTURA: pestaña Avanzado, con el resultado de una simulación]`

- **Modo espejo activado** (solo visible en modo Espejo): recuerda que lo que no
  exista en el origen se elimina del destino — a la papelera de seguridad, no un
  borrado directo — y sugiere simular antes de la primera ejecución real.
- **Usar rutas absolutas** / **Eliminar directorios vacíos en el destino**.
- **Verificación posterior a la copia**: solo tamaño, o tamaño + hash SHA-256 (más
  lenta, pero detecta corrupción que un tamaño igual no vería).
- **Simular**: corre una simulación completa (*dry run*) sin copiar ni borrar nada de
  verdad. Muestra qué se copiaría, qué se omitiría (ya estaba al día) y, en modo
  espejo, qué se eliminaría — con el total de bytes a copiar.
