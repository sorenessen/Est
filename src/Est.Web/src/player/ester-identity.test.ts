import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  resolveEsterIdentity,
  type EsterIdentityStorage,
} from './ester-identity'

function createStorage(
  initial?: string,
): EsterIdentityStorage & {
  value: string | null
} {
  return {
    value: initial ?? null,

    getItem() {
      return this.value
    },

    setItem(
      _key: string,
      value: string,
    ) {
      this.value = value
    },
  }
}

describe(
  'resolveEsterIdentity',
  () => {
    it(
      'reuses a valid persisted identity',
      () => {
        const id =
          '11111111-1111-4111-8111-111111111111'

        const storage =
          createStorage(id)

        expect(
          resolveEsterIdentity(
            storage,
            () =>
              '22222222-2222-4222-8222-222222222222',
          ),
        ).toBe(id)
      },
    )

    it(
      'creates and persists an identity when none exists',
      () => {
        const storage =
          createStorage()

        const created =
          '22222222-2222-4222-8222-222222222222'

        expect(
          resolveEsterIdentity(
            storage,
            () => created,
          ),
        ).toBe(created)

        expect(
          storage.value,
        ).toBe(created)
      },
    )

    it(
      'replaces malformed persisted identity',
      () => {
        const storage =
          createStorage(
            'not-an-ester-id',
          )

        const created =
          '33333333-3333-4333-8333-333333333333'

        expect(
          resolveEsterIdentity(
            storage,
            () => created,
          ),
        ).toBe(created)

        expect(
          storage.value,
        ).toBe(created)
      },
    )
  },
)
