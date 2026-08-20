# ADR-013 — FluentAssertions 8.x pese a su licencia comercial

## Contexto

`CLAUDE.md` §2 fija `FluentAssertions` como la librería de aserciones fluidas para
pruebas. Desde la versión 8.0 (enero de 2025), FluentAssertions cambió de licencia
Apache 2.0 a una licencia comercial de Xceed: sigue siendo gratuita para uso
individual y proyectos open-source, pero exige licencia paga para empresas que
superen cierto tamaño de ingresos. La última versión bajo Apache 2.0 es la 7.2.2.
Existe además un fork comunitario, `AwesomeAssertions`, con el mismo API bajo
licencia MIT permanente.

## Decisión

Se usa **FluentAssertions 8.10.0**, la versión más reciente, con licencia comercial.

## Motivo

Se consultó al autor explícitamente entre las tres opciones (8.x, 7.2.2 congelada, o
el fork MIT) y eligió 8.x: es la que nombra `CLAUDE.md`, y el uso actual del proyecto
(desarrollo individual) cae dentro del tramo gratuito de la licencia de Xceed.

## Consecuencias

- Si el proyecto pasara a un contexto comercial con ingresos por encima del umbral
  de la licencia de Xceed, hay que revisar si sigue aplicando el tramo gratuito o si
  hace falta licenciar, migrar a `AwesomeAssertions` (mismo API, cambia el
  namespace) o congelar en 7.2.2.
- Ningún código de producción depende de FluentAssertions: solo los cuatro proyectos
  de `tests/`. Migrar de librería, si hiciera falta, no toca `src/`.
