Database migrations
===================

Ordered SQL scripts that bring an existing `wavebox.db` up to the current schema. The server
applies any whose version is above the database's own on startup, then records the new version in
the single-row `Version` table.

`../wavebox.sql` is a **frozen baseline at version 0**. Do not edit it to add or change a column —
write a migration instead. A fresh database is the baseline plus every migration replayed in order,
so migrations are exercised by every fresh install and every CI run rather than only being tried
for the first time against somebody's real database.

Naming
------

    00001_add_user_nickname.sql
    00002_index_song_release_year.sql

Five zero-padded digits, an underscore, then a short description of what the migration does. The
padding is so lexical and numeric order agree when you list the directory; the server sorts
numerically either way. Gaps are fine — two branches can merge out of order — but two files sharing
a version number is an error, and so is a name that doesn't match the pattern. Both fail at startup
rather than being skipped silently.

Writing one
-----------

* **No `BEGIN`, `COMMIT` or `ROLLBACK`.** The server wraps each migration in a transaction together
  with its version bump, so either the whole file applies or none of it does.
* Multiple statements per file are fine. Scripts run through SQLite's own parser, so semicolons
  inside string literals, comments and trigger bodies are handled correctly.
* Write them to be safe against a partially-set-up database where you can (`IF NOT EXISTS`), but
  don't contort the SQL for it — a failed migration stops startup with the file name in the log.
* SQLite's `ALTER TABLE` is limited. Anything beyond adding a column or renaming means the usual
  dance: create the new table, `INSERT INTO ... SELECT`, drop the old, rename. That is several
  statements in one file, which is supported.

Once a migration has shipped in a release, treat it as immutable — editing it will not re-run on
databases that already recorded its version.
