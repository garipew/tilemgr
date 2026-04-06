# Tilemgr

> A real-time collaborative tilemap editor built to explore distributed state management on the web.

![demo](assets/demo.gif)

---

## Features

* Real‑time tile editing in the browser
* WebSocket‑based synchronization
* Local project storage
* PNG tileset import
* Binary export format with RLE compression

---

## Export format

Tilemgr allows users to export projects to a custom binary format at any time. The structure of the binary file is very simple:

- **First line:** width and height as space-separated integers
- **Remaining content:** the tilemap data compressed with RLE

The format was designed with the full tool cycle in mind. An editor is only useful if you can consume its output.

---

## Running locally

Requires the .NET 9 SDK.

```bash
git clone https://github.com/garipew/tilemgr
cd tilemgr
dotnet run
```

---

## Projects

Tilemgr organizes work into **projects**.

You can:

* Create a new project
* Browse existing projects
* Open a project directly in the editor

### Creating a project

When creating a new project, you must provide:

* A PNG tileset image
* Tile width (in pixels)
* Tile height (in pixels)
* Map dimensions (columns × rows)

Once created, the project can be edited immediately in the browser.

---

## Editor

The editor is the core of Tilemgr.

* Left‑click to place tiles
* Right‑click to erase tiles
* Tile selection via the palette
* Real‑time updates over WebSockets

The UI is intentionally minimal and optimized for precision rather than decoration.

---

## Status

Tilemgr is a functional tool under active development.

Expect occasional rough edges, but a stable core focused on usability and simplicity.
