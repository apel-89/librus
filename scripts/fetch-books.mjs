#!/usr/bin/env node
/**
 * Hämtar bokdata från Open Library och skriver seed/books.json.
 *
 * Körs en gång manuellt. Resultatet checkas in i repot, så att
 * seedningen aldrig behöver nätverk vid uppstart.
 *
 *   node scripts/fetch-books.mjs
 */

import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const OUTPUT = resolve(ROOT, "backend/src/Librus.Api/Seed/books.json");

// Open Library ber om en identifierande User-Agent.
const USER_AGENT = "Librus/1.0 (kodtest; jonas.apelfjord@example.com)";

const BOOKS_PER_GENRE = 10;

// subject = Open Librarys ämnesnyckel, name = det som hamnar i genres-tabellen.
const GENRES = [
  { subject: "fantasy", name: "Fantasy" },
  { subject: "science_fiction", name: "Science fiction" },
  { subject: "mystery_and_detective_stories", name: "Deckare" },
  { subject: "historical_fiction", name: "Historisk roman" },
  { subject: "biography", name: "Biografi" },
  { subject: "science", name: "Populärvetenskap" },
  { subject: "philosophy", name: "Filosofi" },
  { subject: "fiction", name: "Skönlitteratur" },
];

const SEARCH_FIELDS = [
  "key",
  "title",
  "author_name",
  "first_publish_year",
  "number_of_pages_median",
  "cover_i",
].join(",");

// Open Librarys ämnesindex är brett; några titlar vill man inte ha i ett demobibliotek.
const EXCLUDED_TITLES = ["mein kampf"];
const isExcluded = (title) => {
  const t = title.trim().toLowerCase();
  return EXCLUDED_TITLES.some((x) => t.includes(x));
};

const isLatinScript = (s) =>
  !/[\u3000-\u9fff\uac00-\ud7af\u0400-\u04ff\u0600-\u06ff]/.test(s);

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function getJson(url, attempt = 1) {
  try {
    const res = await fetch(url, { headers: { "User-Agent": USER_AGENT } });
    if (res.status === 429 || res.status >= 500) {
      throw new Error(`HTTP ${res.status}`);
    }
    if (!res.ok) return null;
    return await res.json();
  } catch (err) {
    if (attempt >= 3) {
      console.warn(`  ! ger upp på ${url}: ${err.message}`);
      return null;
    }
    const backoff = 500 * 2 ** (attempt - 1);
    await sleep(backoff);
    return getJson(url, attempt + 1);
  }
}

/** Söker fram kandidater i en genre. Hämtar med marginal eftersom en del faller bort. */
async function searchGenre({ subject, name }) {
  const url =
    `https://openlibrary.org/search.json` +
    `?q=subject:${subject}` +
    `&language=eng` +
    `&sort=readinglog` +
    `&fields=${SEARCH_FIELDS}` +
    `&limit=${BOOKS_PER_GENRE * 3}`;

  const data = await getJson(url);
  if (!data?.docs) {
    console.warn(`  ! inga träffar för ${subject}`);
    return [];
  }

  return (
    data.docs
      .filter((d) => d.key && d.title)
      .filter((d) => Array.isArray(d.author_name) && d.author_name.length > 0)
      .filter((d) => Number.isInteger(d.number_of_pages_median))
      // Orimliga sidantal är nästan alltid samlingsvolymer eller trasig data.
      .filter(
        (d) =>
          d.number_of_pages_median >= 80 && d.number_of_pages_median <= 1200,
      )
      .filter((d) => !isExcluded(d.title))
      .filter((d) => isLatinScript(d.title))
      .map((d) => ({
        workKey: d.key,
        title: d.title.trim(),
        authorName: d.author_name[0].trim(),
        publishedYear: d.first_publish_year,
        pages: d.number_of_pages_median,
        coverId: d.cover_i ?? null,
        genre: name,
      }))
  );
}

/** Baksidestexten ligger inte i sökresultatet utan måste hämtas per verk. */
async function fetchDescription(workKey) {
  const data = await getJson(`https://openlibrary.org${workKey}.json`);
  const raw = data?.description;

  // Fältet är ibland en sträng, ibland { type, value }.
  const text = typeof raw === "string" ? raw : raw?.value;
  if (!text) return null;

  // Open Library har ofta källhänvisningar sist, separerade med ---.
  const cleaned = text
    .split(/\r?\n-{3,}/)[0]
    .replace(/\s+/g, " ")
    .trim();
  return cleaned.length > 20 ? cleaned.slice(0, 1500) : null;
}

async function main() {
  const seen = new Set();
  const books = [];

  for (const genre of GENRES) {
    process.stdout.write(`${genre.name} ... `);
    const candidates = await searchGenre(genre);

    let taken = 0;
    for (const book of candidates) {
      if (taken >= BOOKS_PER_GENRE) break;
      // Samma verk dyker upp i flera ämnen; första genren vinner.
      if (seen.has(book.workKey)) continue;

      book.description = await fetchDescription(book.workKey);
      await sleep(200);

      seen.add(book.workKey);
      books.push(book);
      taken++;
    }

    console.log(`${taken} böcker`);
    await sleep(300);
  }

  const usedGenres = GENRES.map((g) => g.name).filter((name) =>
    books.some((b) => b.genre === name),
  );

  const payload = {
    source: "https://openlibrary.org",
    fetchedAt: new Date().toISOString().slice(0, 10),
    genres: usedGenres,
    books,
  };

  await mkdir(dirname(OUTPUT), { recursive: true });
  await writeFile(OUTPUT, JSON.stringify(payload, null, 2) + "\n", "utf8");

  const withDescription = books.filter((b) => b.description).length;
  console.log(
    `\nSkrev ${books.length} böcker till backend/src/Librus.Api/Seed/books.json ` +
      `(${withDescription} med baksidestext, ${usedGenres.length} genrer).`,
  );
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
