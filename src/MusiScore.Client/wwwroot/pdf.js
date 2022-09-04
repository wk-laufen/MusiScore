import { degrees, PageSizes, PDFDocument } from './lib/pdf-lib/pdf-lib.esm.js'
import './lib/pdf.js/pdf.js'

pdfjsLib.GlobalWorkerOptions.workerSrc = "./lib/pdf.js/pdf.worker.js";

export async function convertDocument(inputContent) {
    const input = await PDFDocument.load(inputContent)
    const output = await PDFDocument.create()
    for (let pageIndex in input.getPageIndices()) {
        const [page] = await output.copyPages(input, [pageIndex])
        page.setCropBox(0, 0, page.getWidth(), page.getHeight() / 2)
        page.setRotation(degrees(90))
        page.scale(2, 2)
        output.addPage(page)
        page.setCropBox(0, page.getHeight() / 2, page.getWidth(), page.getHeight() / 2)
        output.addPage(page)
    }
    return await output.save()
}

async function renderPages(inputContent, container) {
    container.innerHTML = ""

    if (inputContent.length === 0) {
        return
    }
    
    const pdf = await pdfjsLib.getDocument(inputContent).promise
    const outputScale = window.devicePixelRatio || 1
    for (let pageIndex = 1; pageIndex <= pdf.numPages; pageIndex++) {
        const page = await pdf.getPage(pageIndex)

        const viewport = page.getViewport({ scale: 0.5 })

        const canvas = document.createElement('canvas')
        canvas.width = Math.floor(viewport.width * outputScale)
        canvas.height = Math.floor(viewport.height * outputScale)
        container.appendChild(canvas)

        const context = canvas.getContext('2d')

        const transform = outputScale !== 1 ? [outputScale, 0, 0, outputScale, 0, 0] : null

        await page.render({ canvasContext: context, viewport: viewport, transform: transform })
    }
}

export async function renderVoice(voice) {
    const container = document.querySelector('.voice-preview')
    if (!container) {
        throw `Can't render voice preview: Container not found (".voice-preview")`
    }

    await renderPages(voice.file, container)
}

// async function readFile(file) {
//     return new Promise((resolve, reject) => {
//         const reader = new FileReader()
//         reader.onload = () => {
//             const result = new Uint8Array(reader.result)
//             resolve(result)
//         }
//         reader.onerror = reject
//         reader.readAsArrayBuffer(file)
//     })
// }

export async function validatePdfFile(content) {
    await PDFDocument.load(content)
}
