export const joinStrings = (items: string[]) => {
  if (items.length === 0) return ''
  const length = items.length > 3 ? 2 : items.length
  const parts = [
    ...items.slice(0, length),
    ...(items.length > length ? [`${items.length - length} weitere`] : [])
  ]
  const lastItem = parts.pop()
  const result = [
    ...(parts.length > 0 ? [parts.join(', ')] : []),
    ...(lastItem !== undefined ? [lastItem] : [])
  ]
  return result.join(' und ')
}

export const downloadFile = (blob: Blob, name: string) => {
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  a.click()
}
