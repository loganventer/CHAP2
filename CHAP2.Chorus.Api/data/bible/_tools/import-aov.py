#!/usr/bin/env python3
"""
One-shot importer: turns the raw getBible.net AOV JSON into the
per-chapter file layout used by CHAP2's DiskBibleRepository.

Input  : /tmp/aov-raw.json (download from https://api.getbible.net/v2/aov.json)
Output : data/bible/aov/_books.json + data/bible/aov/{ordinal:02d}-{slug}/{chapter:03d}.json

Run from anywhere:
    python3 data/bible/_tools/import-aov.py
"""
import json
import sys
import unicodedata
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
RAW_PATH = Path("/tmp/aov-raw.json")
OUT_DIR = REPO_ROOT / "data" / "bible" / "aov"

AFRIKAANS_BOOKS = [
    (1, "Genesis", "Genesis", "Old"),
    (2, "Eksodus", "Exodus", "Old"),
    (3, "Levitikus", "Leviticus", "Old"),
    (4, "Numeri", "Numbers", "Old"),
    (5, "Deuteronomium", "Deuteronomy", "Old"),
    (6, "Josua", "Joshua", "Old"),
    (7, "Rigters", "Judges", "Old"),
    (8, "Rut", "Ruth", "Old"),
    (9, "1 Samuel", "1 Samuel", "Old"),
    (10, "2 Samuel", "2 Samuel", "Old"),
    (11, "1 Konings", "1 Kings", "Old"),
    (12, "2 Konings", "2 Kings", "Old"),
    (13, "1 Kronieke", "1 Chronicles", "Old"),
    (14, "2 Kronieke", "2 Chronicles", "Old"),
    (15, "Esra", "Ezra", "Old"),
    (16, "Nehemia", "Nehemiah", "Old"),
    (17, "Ester", "Esther", "Old"),
    (18, "Job", "Job", "Old"),
    (19, "Psalms", "Psalms", "Old"),
    (20, "Spreuke", "Proverbs", "Old"),
    (21, "Prediker", "Ecclesiastes", "Old"),
    (22, "Hooglied", "Song of Solomon", "Old"),
    (23, "Jesaja", "Isaiah", "Old"),
    (24, "Jeremia", "Jeremiah", "Old"),
    (25, "Klaagliedere", "Lamentations", "Old"),
    (26, "Eseg\u00ebl", "Ezekiel", "Old"),
    (27, "Dani\u00ebl", "Daniel", "Old"),
    (28, "Hosea", "Hosea", "Old"),
    (29, "Jo\u00ebl", "Joel", "Old"),
    (30, "Amos", "Amos", "Old"),
    (31, "Obadja", "Obadiah", "Old"),
    (32, "Jona", "Jonah", "Old"),
    (33, "Miga", "Micah", "Old"),
    (34, "Nahum", "Nahum", "Old"),
    (35, "Habakuk", "Habakkuk", "Old"),
    (36, "Sefanja", "Zephaniah", "Old"),
    (37, "Haggai", "Haggai", "Old"),
    (38, "Sagaria", "Zechariah", "Old"),
    (39, "Maleagi", "Malachi", "Old"),
    (40, "Matteus", "Matthew", "New"),
    (41, "Markus", "Mark", "New"),
    (42, "Lukas", "Luke", "New"),
    (43, "Johannes", "John", "New"),
    (44, "Handelinge", "Acts", "New"),
    (45, "Romeine", "Romans", "New"),
    (46, "1 Korinti\u00ebrs", "1 Corinthians", "New"),
    (47, "2 Korinti\u00ebrs", "2 Corinthians", "New"),
    (48, "Galasi\u00ebrs", "Galatians", "New"),
    (49, "Efesi\u00ebrs", "Ephesians", "New"),
    (50, "Filippense", "Philippians", "New"),
    (51, "Kolossense", "Colossians", "New"),
    (52, "1 Tessalonisense", "1 Thessalonians", "New"),
    (53, "2 Tessalonisense", "2 Thessalonians", "New"),
    (54, "1 Timoteus", "1 Timothy", "New"),
    (55, "2 Timoteus", "2 Timothy", "New"),
    (56, "Titus", "Titus", "New"),
    (57, "Filemon", "Philemon", "New"),
    (58, "Hebre\u00ebrs", "Hebrews", "New"),
    (59, "Jakobus", "James", "New"),
    (60, "1 Petrus", "1 Peter", "New"),
    (61, "2 Petrus", "2 Peter", "New"),
    (62, "1 Johannes", "1 John", "New"),
    (63, "2 Johannes", "2 John", "New"),
    (64, "3 Johannes", "3 John", "New"),
    (65, "Judas", "Jude", "New"),
    (66, "Openbaring", "Revelation", "New"),
]


def slugify(name: str) -> str:
    norm = unicodedata.normalize("NFKD", name)
    ascii_only = "".join(c for c in norm if not unicodedata.combining(c))
    return ascii_only.lower().replace(" ", "-")


def main() -> int:
    if not RAW_PATH.exists():
        print(f"missing {RAW_PATH} - download with:", file=sys.stderr)
        print("  curl -sSfL -o /tmp/aov-raw.json https://api.getbible.net/v2/aov.json", file=sys.stderr)
        return 1

    raw = json.loads(RAW_PATH.read_text(encoding="utf-8"))
    raw_books = raw["books"]
    if len(raw_books) != 66:
        print(f"expected 66 books, got {len(raw_books)}", file=sys.stderr)
        return 1

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    books_index = []
    total_chapters = 0
    total_verses = 0

    for ordinal, af_name, en_name, testament in AFRIKAANS_BOOKS:
        raw_book = raw_books[ordinal - 1]
        chapters = raw_book["chapters"]
        slug = slugify(af_name)
        book_id = slug
        book_dir_name = f"{ordinal:02d}-{slug}"
        book_dir = OUT_DIR / book_dir_name
        book_dir.mkdir(parents=True, exist_ok=True)

        for ch in chapters:
            ch_num = int(ch["chapter"])
            # Preserve original formatting verbatim (trailing spaces, embedded
            # newlines, etc.) so the chapter renders the way the source set it.
            verses = [
                {"verse": int(v["verse"]), "text": v["text"]}
                for v in ch["verses"]
            ]
            chapter_doc = {
                "bookId": book_id,
                "bookName": af_name,
                "chapter": ch_num,
                "verses": verses,
            }
            (book_dir / f"{ch_num:03d}.json").write_text(
                json.dumps(chapter_doc, ensure_ascii=False, indent=2) + "\n",
                encoding="utf-8",
            )
            total_verses += len(verses)
        total_chapters += len(chapters)

        books_index.append({
            "id": book_id,
            "name": af_name,
            "englishName": en_name,
            "ordinal": ordinal,
            "testament": testament,
            "chapterCount": len(chapters),
            "directory": book_dir_name,
        })

    (OUT_DIR / "_books.json").write_text(
        json.dumps(books_index, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    print(f"wrote {len(books_index)} books, {total_chapters} chapters, {total_verses} verses to {OUT_DIR}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
