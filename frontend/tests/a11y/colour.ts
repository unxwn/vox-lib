import { inflateSync } from 'node:zlib'

/**
 * Just enough PNG to read the pixels back out of a Playwright screenshot, and
 * the WCAG luminance arithmetic to say something about them.
 *
 * The ground's whole obligation is a claim about rendered pixels: the grain has
 * to stay inside a declared luminance band. That cannot be computed from the
 * tokens, because what the band contains is the composite of a filter, an
 * opacity and a repeat, so it has to be measured off the real thing. Playwright
 * hands back a PNG buffer and there is no image decoder in this project's
 * dependencies, so the decoder is here rather than a package: it reads exactly
 * the one shape Chromium emits, 8-bit non-interlaced RGB or RGBA, and refuses
 * anything else instead of guessing.
 */

export type Pixels = {
  readonly width: number
  readonly height: number
  /** RGBA, four bytes per pixel, row-major. */
  readonly data: Buffer
}

function paeth(a: number, b: number, c: number): number {
  const p = a + b - c
  const pa = Math.abs(p - a)
  const pb = Math.abs(p - b)
  const pc = Math.abs(p - c)

  if (pa <= pb && pa <= pc) {
    return a
  }

  return pb <= pc ? b : c
}

export function decodePng(png: Buffer): Pixels {
  const signature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])

  if (!png.subarray(0, 8).equals(signature)) {
    throw new Error('not a PNG')
  }

  let at = 8
  let header: { width: number; height: number; channels: number } | undefined
  const idat: Buffer[] = []

  while (at < png.length) {
    const length = png.readUInt32BE(at)
    const type = png.toString('ascii', at + 4, at + 8)
    const body = png.subarray(at + 8, at + 8 + length)

    if (type === 'IHDR') {
      const bitDepth = body.readUInt8(8)
      const colourType = body.readUInt8(9)
      const interlace = body.readUInt8(12)

      if (bitDepth !== 8 || interlace !== 0 || (colourType !== 2 && colourType !== 6)) {
        throw new Error(
          `unsupported PNG: bit depth ${bitDepth}, colour type ${colourType}, interlace ${interlace}`,
        )
      }

      header = {
        width: body.readUInt32BE(0),
        height: body.readUInt32BE(4),
        channels: colourType === 6 ? 4 : 3,
      }
    } else if (type === 'IDAT') {
      idat.push(body)
    } else if (type === 'IEND') {
      break
    }

    at += 12 + length
  }

  if (header === undefined) {
    throw new Error('PNG has no IHDR')
  }

  const { width, height, channels } = header
  const raw = inflateSync(Buffer.concat(idat))
  const stride = width * channels
  const out = Buffer.alloc(width * height * 4)
  const line = Buffer.alloc(stride)
  const previous = Buffer.alloc(stride)

  for (let row = 0; row < height; row++) {
    const start = row * (stride + 1)
    const filter = raw.readUInt8(start)

    raw.copy(line, 0, start + 1, start + 1 + stride)

    for (let index = 0; index < stride; index++) {
      const left = index >= channels ? line[index - channels] : 0
      const up = previous[index]
      const upLeft = index >= channels ? previous[index - channels] : 0

      if (filter === 1) {
        line[index] = (line[index] + left) & 0xff
      } else if (filter === 2) {
        line[index] = (line[index] + up) & 0xff
      } else if (filter === 3) {
        line[index] = (line[index] + ((left + up) >> 1)) & 0xff
      } else if (filter === 4) {
        line[index] = (line[index] + paeth(left, up, upLeft)) & 0xff
      } else if (filter !== 0) {
        throw new Error(`unknown PNG row filter ${filter}`)
      }
    }

    for (let column = 0; column < width; column++) {
      const from = column * channels
      const to = (row * width + column) * 4

      out[to] = line[from]
      out[to + 1] = line[from + 1]
      out[to + 2] = line[from + 2]
      out[to + 3] = channels === 4 ? line[from + 3] : 0xff
    }

    line.copy(previous)
  }

  return { width, height, data: out }
}

/** WCAG 2.x relative luminance, from three 8-bit channels. */
export function luminance(r: number, g: number, b: number): number {
  const channel = (eight: number) => {
    const c = eight / 255
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  }

  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)
}

/** `#rrggbb` or `rgb(r, g, b)` as computed style hands it back. */
export function luminanceOf(colour: string): number {
  const hex = /^#([0-9a-f]{6})$/i.exec(colour.trim())

  if (hex !== null) {
    const [r, g, b] = [0, 2, 4].map((at) => parseInt(hex[1].slice(at, at + 2), 16))
    return luminance(r, g, b)
  }

  const rgb = /^rgba?\(\s*([\d.]+)[\s,]+([\d.]+)[\s,]+([\d.]+)/i.exec(colour.trim())

  if (rgb === null) {
    throw new Error(`cannot read the colour ${colour}`)
  }

  return luminance(Number(rgb[1]), Number(rgb[2]), Number(rgb[3]))
}
