import _ from "lodash"
import { PageSizes, PDFContentStream, PDFDocument, popGraphicsState, pushGraphicsState, rotateDegrees, scale, translate } from "pdf-lib"

export type PdfModification = {
  type: 'scaleToA4',
  pages: readonly number[]
} | {
  type: 'zoom',
  relativeBounds: { x: number, y: number, width: number, height: number },
  pages: readonly number[]
} | {
  type: 'remove',
  pages: readonly number[]
} | {
  type: 'rotate',
  degrees: number,
  pages: readonly number[]
} | {
  type: 'cutPageLeftRight',
  pages: readonly number[]
}

export type PDFFile = {
  data: Uint8Array
  pageCount: number
}

export module Pdf {
  export const applyModifications = async (doc: Uint8Array, modifications: PdfModification[]) : Promise<PDFFile> => {
    let pdfDoc = await PDFDocument.load(doc)
    for (const modification of modifications) {
      pdfDoc = await applyModification(pdfDoc, modification)
    }
    return {
      data: await pdfDoc.save(),
      pageCount: pdfDoc.getPageCount()
    }
  }

  const applyModification = async (doc: PDFDocument, modification: PdfModification) : Promise<PDFDocument> => {
    const modifiedDoc = await doc.copy()
    switch (modification.type) {
      case "scaleToA4":
        scalePagesToA4(modifiedDoc, modification.pages)
        return modifiedDoc
      case "zoom":
        zoom(modifiedDoc, modification.pages, modification.relativeBounds)
        return modifiedDoc
      case "remove":
        removePages(modifiedDoc, modification.pages)
        return modifiedDoc
      case "rotate":
        rotatePages(modifiedDoc, modification.pages, modification.degrees)
        return modifiedDoc
      case "cutPageLeftRight":
        cutPageLeftRight(modifiedDoc, modification.pages)
        return modifiedDoc
    }
  }

  const scalePagesToA4 = (doc: PDFDocument, pageNumbers: readonly number[]) => {
    for (const pageNumber of pageNumbers) {
      const page = doc.getPage(pageNumber)
      const { ratio, translateX, translateY } = getScaleRatio(page.getSize(), { width: PageSizes.A4[0], height: PageSizes.A4[1] })
      page.setSize(...PageSizes.A4)
      page.scaleContent(ratio, ratio)
      page.translateContent(translateX, translateY)
    }
  }

  const zoom = (doc: PDFDocument, pageNumbers: readonly number[], relativeBounds: {x: number, y: number, width: number, height: number}) => {
    for (const pageNumber of pageNumbers) {
      const page = doc.getPage(pageNumber)
      const bounds = {
        x: relativeBounds.x * page.getWidth(),
        y: relativeBounds.y * page.getHeight(),
        width: relativeBounds.width * page.getWidth(),
        height: relativeBounds.height * page.getHeight()
      }
      const { ratio, translateX, translateY } = getScaleRatio(bounds, page.getSize())
      page.translateContent(-bounds.x - bounds.width / 2, -bounds.y - bounds.height / 2)
      page.scaleContent(ratio, ratio)
      page.translateContent(translateX + bounds.width / 2 * ratio, translateY + bounds.height / 2 * ratio)
    }
  }

  const removePages = (doc: PDFDocument, pageNumbers: readonly number[]) => {
    for (const pageNumber of _.orderBy(pageNumbers, v => v, 'desc')) {
      doc.removePage(pageNumber)
    }
  }

  const rotatePages = (doc: PDFDocument, pageNumbers: readonly number[], degrees: number) => {
    for (const pageNumber of pageNumbers) {
      const page = doc.getPage(pageNumber)

      const rotatedPageBox = getBoundingBox(rotateRectangle({ x: 0, y: 0, ...page.getSize() }, degrees))
      const { ratio } = getScaleRatio(rotatedPageBox, page.getSize())

      page.node.normalize()
      const startOperations = [
        pushGraphicsState(),
        translate(page.getWidth() / 2, page.getHeight() / 2),
        rotateDegrees(-degrees),
        scale(ratio, ratio),
        translate(-page.getWidth() / 2, -page.getHeight() / 2),
      ]
      const endOperations = [ popGraphicsState() ]
      page.node.wrapContentStreams(
        page.doc.context.register(PDFContentStream.of(page.doc.context.obj({}), startOperations)),
        page.doc.context.register(PDFContentStream.of(page.doc.context.obj({}), endOperations))
      )
    }
  }

  const cutPageLeftRight = async (doc: PDFDocument, pageNumbers: readonly number[]) => {
    const pages = await doc.copyPages(doc, pageNumbers.slice())
    for (const [index, page] of _(_.zip(pageNumbers, pages)).orderBy(([index, _]) => index, 'desc').value()) {
      if (index === undefined || page === undefined) continue
      const centerX = page.getWidth() / 2
      const page1 = doc.insertPage(index, page)
      page1.setSize(centerX, page.getHeight())
      const page2 = doc.getPage(index + 1)
      page2.translateContent(-centerX, 0)
      page2.setSize(centerX, page.getHeight())
    }
  }

  const getScaleRatio = (originalSize: {width: number, height: number}, targetSize: {width: number, height: number}) => {
    const ratio = Math.min(targetSize.width / originalSize.width, targetSize.height / originalSize.height)
    const [translateX, translateY] = [ (targetSize.width - originalSize.width * ratio) / 2, (targetSize.height - originalSize.height * ratio) / 2 ]
    return { ratio, translateX, translateY }
  }

  const getBoundingBox = (points: {x: number, y: number}[]) => {
    const minX = Math.min(...points.map(v => v.x))
    const maxX = Math.max(...points.map(v => v.x))
    const minY = Math.min(...points.map(v => v.y))
    const maxY = Math.max(...points.map(v => v.y))
    return { x: minX, y: minY, width: maxX - minX, height: maxY - minY }
  }

  const rotateRectangle = (r: {x: number, y: number, width: number, height: number}, degrees: number) => {
    const center = { x: r.x + r.width / 2, y: r.y + r.height / 2 }
    return [
      rotatePoint({x: r.x, y: r.y}, center, degrees),
      rotatePoint({x: r.x + r.width, y: r.y}, center, degrees),
      rotatePoint({x: r.x + r.width, y: r.y + r.height}, center, degrees),
      rotatePoint({x: r.x, y: r.y + r.height}, center, degrees)
    ]
  }

  // see https://en.wikipedia.org/wiki/Rotation_matrix
  const rotatePoint = (p: {x: number, y: number}, center: {x: number, y: number}, degrees: number) => {
    const radians = -degrees * Math.PI / 180
    const dx = p.x - center.x
    const dy = p.y - center.y
    return {
      x: center.x + dx * Math.cos(radians) - dy * Math.sin(radians),
      y: center.y + dx * Math.sin(radians) + dy * Math.cos(radians)
    }
  }
}
