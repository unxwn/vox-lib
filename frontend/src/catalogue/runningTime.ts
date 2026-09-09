/**
 * A running time in words, in Ukrainian.
 *
 * Written out rather than shown as 1:05:00, because a screen reader reads a
 * colon-separated clock as a time of day or as a run of digits, and "одна
 * година п'ять хвилин" is what a listener actually wants to know. Seconds are
 * dropped above a minute: nobody chooses a chapter on seven seconds.
 */
export function formatRunningTime(totalSeconds: number): string {
  if (totalSeconds <= 0) {
    return '0 хв'
  }

  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.round((totalSeconds % 3600) / 60)

  // Rounding can carry a chapter of 59 minutes 40 seconds up to a full hour,
  // which would otherwise read as "1 год 60 хв".
  const carried = minutes === 60
  const displayHours = carried ? hours + 1 : hours
  const displayMinutes = carried ? 0 : minutes

  if (displayHours === 0) {
    return `${displayMinutes} хв`
  }

  return displayMinutes === 0 ? `${displayHours} год` : `${displayHours} год ${displayMinutes} хв`
}
