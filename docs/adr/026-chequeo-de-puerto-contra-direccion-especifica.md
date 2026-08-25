# ADR-026 — El chequeo de puerto libre prueba la dirección específica, no `0.0.0.0`

## Contexto

Al probar a mano el endpoint nuevo `GET /api/settings/web/port-check` (RF-111, sesión
3C) contra la API real corriendo — con Kestrel escuchando en `127.0.0.1:8483` — pedir
el estado del puerto 8483 (el que la API ya estaba usando en ese mismo momento)
devolvió `isAvailable: true`. El chequeo original abría un `TcpListener` en
`IPAddress.Any` (`0.0.0.0`) para probar el puerto. En Windows, un bind a `0.0.0.0:P`
**no** entra en conflicto con otro proceso que ya esté escuchando en
`127.0.0.1:P` específicamente — son ámbitos de bind distintos — así que el chequeo
casi siempre daba "libre", incluso con la propia instancia de JMBackup usándolo.

## Decisión

`CheckPortAvailability` recibe la dirección de escucha a probar (`GET
.../port-check?port=X&address=Y`) y arma el `TcpListener` contra esa dirección
específica, no contra `IPAddress.Any`. Sin `address` en la query, el endpoint usa la
`ListenAddress` ya guardada en `Settings` — la dirección contra la que Kestrel está
escuchando de verdad en este momento — como default razonable.

## Motivo

El chequeo solo tiene sentido si prueba el mismo ámbito de bind que Kestrel va a usar
de verdad. Probarlo contra `0.0.0.0` responde una pregunta distinta ("¿hay algo
escuchando en *todas* las direcciones en este puerto?") de la que RF-111 hace ("¿puedo
escuchar en el puerto configurado?").

## Consecuencias

- El endpoint necesita `SettingsService` para resolver el default cuando no se pasa
  `address` explícito — antes no tenía ninguna dependencia.
- Si en el futuro la pantalla de Web permite cambiar `ListenAddress` y `Port` a la vez,
  el chequeo puede probar la combinación nueva pasando `address` explícito antes de
  guardar, sin esperar a que el usuario guarde primero.
