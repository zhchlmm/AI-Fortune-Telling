const timezonePattern = /(Z|[+-]\d{2}:\d{2})$/i

function parseBackendUtc(value: string) {
  if (!value) {
    return new Date(Number.NaN)
  }

  const normalized = timezonePattern.test(value) ? value : `${value}Z`
  return new Date(normalized)
}

function formatWithTimezone(date: Date, includeSeconds: boolean) {
  if (Number.isNaN(date.getTime())) {
    return '-'
  }

  const formatter = new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: includeSeconds ? '2-digit' : undefined,
    hour12: false,
  })

  const parts = formatter.formatToParts(date)
  const map: Record<string, string> = {}
  for (const part of parts) {
    map[part.type] = part.value
  }

  const base = `${map.year}-${map.month}-${map.day} ${map.hour}:${map.minute}`
  if (!includeSeconds) {
    return base
  }

  return `${base}:${map.second}`
}

export function toUtcTimestamp(value: string) {
  return parseBackendUtc(value).getTime()
}

export function formatBeijingDateTime(value: string) {
  return formatWithTimezone(parseBackendUtc(value), true)
}

export function formatBeijingCompactDateTime(value: string) {
  return formatWithTimezone(parseBackendUtc(value), false)
}
