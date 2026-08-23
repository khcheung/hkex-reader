# hkex-reader
HKEX Reader

## Command Line

Working Dir: /HKEXReader/

OLD COMMAND: dotnet run [STOCKCODE] [OUTPUT FOLDER] [TARGET DATE]
NEW COMMAND: dotnet run download [STOCKCODE] [-o OUTPUT FOLDER] [-d TARGET DATE]

```sh

## OLD ## dotnet run #####,##### ../output yyyy-MM-dd
dotnet run download #####,##### -o ../output -d yyyy-MM-dd

## OLD ## dotnet run 00001,00002,00003,00005,00008,00017 ../output 2026-06-30
dotnet run download 00001,00002,00003,00005,00008,00017 -o ../output -d 2026-06-30

```

## Retrieve Stock List

```sh

## OLD ## dotnet run STOCKCODE ../output
dotnet run download STOCKCODE -o ../output

```
