import { es, type Strings } from './es'

/**
 * En el hito 1 el idioma está fijo en español (RF-122): esta función es el único
 * lugar que sabrá elegir entre `es`/`en` cuando exista el selector de idioma.
 */
export function useStrings(): Strings {
  return es
}
