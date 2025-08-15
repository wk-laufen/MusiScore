import { expect, test } from "vitest"
import { joinStrings } from "./UI"

test('joinStrings should work with 1 item', () => {
  expect(joinStrings(['1'])).toBe('1')
})

test('joinStrings should work with 2 items', () => {
  expect(joinStrings(['1', '2'])).toBe('1 und 2')
})

test('joinStrings should work with 3 items', () => {
  expect(joinStrings(['1', '2', '3'])).toBe('1, 2 und 3')
})

test('joinStrings should work with 4 items', () => {
  expect(joinStrings(['1', '2', '3', '4'])).toBe('1, 2 und 2 weitere')
})
