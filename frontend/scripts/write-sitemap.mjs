#!/usr/bin/env node
/**
 * Writes the sitemap, from the same discovery the prerender uses.
 *
 * Importing `catalogue-paths.mjs` rather than walking the API again is the whole
 * point: FR-032 and FR-058 are one list of addresses seen from two sides, and
 * discovering it twice is how a sitemap comes to advertise a page the build
 * never emitted.
 *
 * It runs after the build, because it needs the same API the prerender needed
 * and there is no reason to ask twice in one command.
 */

import { writeFile } from 'node:fs/promises'
import { join } from 'node:path'
import { cataloguePaths } from './catalogue-paths.mjs'

const origin = (process.env.VOXLIB_SITE_ORIGIN ?? 'http://localhost:5173').replace(/\/+$/, '')
const output = join(process.cwd(), 'build', 'client', 'sitemap.xml')

const escape = (value) => value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

const paths = await cataloguePaths()

const body = paths
  .map((path) => `  <url>\n    <loc>${escape(origin + path)}</loc>\n  </url>`)
  .join('\n')

await writeFile(
  output,
  `<?xml version="1.0" encoding="UTF-8"?>\n` +
    `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${body}\n</urlset>\n`,
  'utf8',
)

console.log(`sitemap: ${paths.length} addresses -> ${output}`)
