export interface EsterIdentityStorage {
  getItem(key: string): string | null
  setItem(key: string, value: string): void
}

const esterIdentityStorageKey =
  'est.ester-id'

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

export function resolveEsterIdentity(
  storage: EsterIdentityStorage,
  createIdentity: () => string,
): string {
  const existing =
    storage.getItem(
      esterIdentityStorageKey,
    )

  if (
    existing !== null &&
    uuidPattern.test(existing)
  ) {
    return existing
  }

  const created =
    createIdentity()

  if (!uuidPattern.test(created)) {
    throw new Error(
      'Generated Ester identity is not a valid UUID.',
    )
  }

  storage.setItem(
    esterIdentityStorageKey,
    created,
  )

  return created
}
