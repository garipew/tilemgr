# Tilemgr
Tilemgr is a self-hosted, real-time tilemap editor that uses a browser as UI,
designed for explicit networking and local-first operations.

## Running
```
dotnet run
```

## Projects
Start a new project or import an existing one.

New projects expect to recieve a PNG of a tilesheet and the dimensions of individual
tiles. After that, you have the control.

## Exporting
Tilemgr exports projects to a binary format that consists in the tile numbers
encoded with RLE.
