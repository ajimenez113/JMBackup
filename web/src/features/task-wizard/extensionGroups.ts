export interface ExtensionGroup {
  key: string
  label: string
  extensions: string[]
}

/** RF-40: formatos comunes agrupados, más un campo manual para el resto. */
export const EXTENSION_GROUPS: ExtensionGroup[] = [
  { key: 'video', label: 'Video', extensions: ['mp4', 'mkv', 'avi', 'mov', 'wmv', 'flv', 'webm'] },
  { key: 'image', label: 'Imagen', extensions: ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'svg', 'webp', 'heic'] },
  { key: 'audio', label: 'Audio', extensions: ['mp3', 'wav', 'flac', 'aac', 'ogg', 'wma', 'm4a'] },
  { key: 'office', label: 'Ofimática', extensions: ['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'pdf', 'odt'] },
  { key: 'archive', label: 'Comprimidos', extensions: ['zip', 'rar', '7z', 'tar', 'gz', 'iso'] },
  { key: 'executable', label: 'Ejecutables', extensions: ['exe', 'msi', 'bat', 'cmd', 'ps1'] },
  { key: 'temporary', label: 'Temporales', extensions: ['tmp', 'temp', 'bak', 'log', 'cache'] },
]
