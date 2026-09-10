type StatusRegionProps = {
  /**
   * `status` for progress and success, which a screen reader announces when it
   * next pauses. `alert` for a failure, which interrupts, because the person
   * asked for something and did not get it.
   */
  tone: 'status' | 'alert'
  children: React.ReactNode
}

/**
 * Announces a change of state a sighted person would notice: submitting,
 * failing, succeeding, confirming, signing out. FR-033.
 *
 * The region is always in the document and only its contents change. A live
 * region added to the page at the moment it has something to say is frequently
 * missed, because the reader has nothing to compare against.
 */
export function StatusRegion({ tone, children }: StatusRegionProps) {
  return (
    <div
      className={`status status--${tone}`}
      role={tone}
      aria-live={tone === 'alert' ? 'assertive' : 'polite'}
      aria-atomic="true"
    >
      {children}
    </div>
  )
}
