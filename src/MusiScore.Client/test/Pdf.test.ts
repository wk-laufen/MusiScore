import { expect, test } from 'vitest'
import { Pdf } from '../src/components/Pdf'
import { PDFDocument } from 'pdf-lib'
import path from 'path'
import fs from 'fs/promises'

test('PDF is smaller when removing a page', async () => {
  const content = await fs.readFile(path.resolve(__dirname, './Doc1.pdf'))
  const doc = await PDFDocument.load(content.buffer as ArrayBuffer)
  const out = await Pdf.applyModifications(await doc.save(), [
    {
      type: 'remove',
      pages: [2]
    }
  ])
  const outDoc = await PDFDocument.load(out.data)
  expect(outDoc.getPageCount()).toBe(2)
  expect(out.data.length).toBeLessThan(content.length * 0.7)
})