import { useEffect, useState } from 'react'
import { fetchPasswordPolicy, type PasswordPolicy } from '../api/account'

/**
 * The password requirements, read from the API rather than written twice.
 *
 * FR-002 asks for them to be available before the form is submitted, not only
 * after a failure. The alternative is the same numbers in a Ukrainian sentence
 * here and in the API's configuration, drifting the first time either changes,
 * and the drift is silent: the sentence would keep saying eight while the rule
 * rejected at twelve.
 */
export function usePasswordPolicy(): PasswordPolicy | undefined {
  const [policy, setPolicy] = useState<PasswordPolicy>()

  useEffect(() => {
    let cancelled = false

    void fetchPasswordPolicy().then((result) => {
      if (!cancelled && result.ok) {
        setPolicy(result.value)
      }
    })

    return () => {
      cancelled = true
    }
  }, [])

  return policy
}

/** The requirements as a sentence, built from whatever the API said they are. */
export function describePolicy(policy: PasswordPolicy | undefined): string {
  if (policy === undefined) {
    return 'Пароль має бути достатньо довгим.'
  }

  const parts = [`щонайменше ${policy.minimumLength} символів`]

  if (policy.requiresDigit) parts.push('хоча б одну цифру')
  if (policy.requiresUppercase) parts.push('хоча б одну велику літеру')
  if (policy.requiresLowercase) parts.push('хоча б одну малу літеру')
  if (policy.requiresNonAlphanumeric) parts.push('хоча б один спеціальний символ')

  return `Пароль має містити ${parts.join(', ')}. Підійде будь-яка мова, зокрема українська.`
}
