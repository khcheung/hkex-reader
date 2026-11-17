# hkex-reader
HKEX Reader

## Command Line

Working Dir: /HKEXReader/

OLD COMMAND: dotnet run [STOCKCODE] [OUTPUT FOLDER] [TARGET DATE]
NEW COMMAND: dotnet run download [STOCKCODE] [-o OUTPUT FOLDER] [-d TARGET DATE] [-s SQLITE PATH]

```sh

## OLD ## dotnet run #####,##### ../output yyyy-MM-dd
dotnet run download #####,##### -o ../output -d yyyy-MM-dd

## OLD ## dotnet run 00001,00002,00003,00005,00008,00017 ../output 2026-06-30
dotnet run download 00001,00002,00003,00005,00008,00017 -o ../output -d 2026-06-30

## Also store snapshots, summary, concentration, and stock list in SQLite
dotnet run download 00001,00002,00003,00005,00008,00017 -o ../output -d 2026-06-30 -s ../output/hkex.db

```

## Retrieve Stock List

```sh

## OLD ## dotnet run STOCKCODE ../output
dotnet run download STOCKCODE -o ../output

## Also store stock list in SQLite
dotnet run download STOCKCODE -o ../output -s ../output/hkex.db

```

## SQLite option

Pass `-s` / `--sqlite <PATH>` to also write downloaded data into a SQLite database (CSV/Markdown output is unchanged).

Tables: `snapshots`, `holdings`, `summary`, `concentration`, `stock_list`.
