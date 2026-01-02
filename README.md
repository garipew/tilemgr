# Tilemgr

Tilemgr is a **self‑hosted, real‑time tilemap editor** that runs locally and uses a web browser as its UI.

It is designed to be:

* **Local‑first** — no external services required
* **Explicitly networked** — real‑time updates via WebSockets
* **Tool‑oriented** — focused on fast iteration rather than presentation

Tilemgr is intended for developers who want a lightweight, hackable tile editor that integrates easily into custom pipelines.

---

## Features

* Real‑time tile editing in the browser
* WebSocket‑based synchronization
* Local project storage
* PNG tileset import
* Binary export format with RLE compression
* Minimal UI, editor‑first workflow

---

## Running

Requires the .NET SDK.

```bash
dotnet run
```

By default, the server hosts the editor locally. Open the provided address in your browser to begin.

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

## Exporting

Projects can be exported to a compact binary format suitable for custom engines or tooling.

### Binary format

The exported file consists of:

1. **Header** — 8 bytes total

   * Width  (32‑bit integer, little‑endian)
   * Height (32‑bit integer, little‑endian)

2. **Tile data**

   * Tile indices encoded using **Run‑Length Encoding (RLE)**

This format is designed to be trivial to parse and efficient to load.

---

## Philosophy

Tilemgr intentionally avoids:

* Cloud dependencies
* Account systems
* Heavy frameworks
* Overdesigned UI layers

The goal is to provide a **reliable, inspectable tool** that does one thing well: editing tilemaps.

---

## Status

Tilemgr is a functional tool under active development.

Expect occasional rough edges, but a stable core focused on usability and simplicity.
