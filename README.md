# Tilemgr

> A real-time collaborative tilemap editor built to explore distributed state management on the web.

![demo](assets/demo.gif)

---

## About

Tilemgr is a real-time collaborative tilemap editor. Create projects, edit together with other users and export them.

Tilemgr is a web project built to explore the most popular technologies on the .NET ecosystem. It is built on a `RESTful API` architecture and provides `CRUD` functionality.

The app leverages the following technologies:

- C#, .NET Core, ASP.NET Core
- EntityFramework Core (EF Core), LINQ
- WebSockets
- PostgreSQL

## Getting started

Requires the .NET 9 SDK.

```bash
git clone https://github.com/garipew/tilemgr
cd tilemgr
dotnet run
```

Then open `http://localhost:5273` in your browser.

## Export format

Tilemgr allows users to export projects to a custom binary format at any time. The structure of the binary file is:

- **First line:** width and height as space-separated integers
- **Remaining content:** the tilemap data compressed with RLE

The format was designed with the full cycle of the tool in mind. An editor is only useful if you can consume its output.

## Roadmap
### Front-end

- [ ] Improve UI on tile picker, highlight selected tile
- [ ] Add navigation on tilemap viewport

### Extra

- [ ] Display cursor position of users on tilemap

## Changelog

- Save on DB and unload project when last user disconnects ([commit](https://github.com/garipew/tilemgr/commit/0ec71553d0bc15f3630131291b981f36cdacfffe))
- Share projects in-memory to reduce DB interactions ([commit](https://github.com/garipew/tilemgr/commit/689ad0d624f86faa1d5419ae09f367a580cacd92))
- Redirect to new project at creation ([commit](https://github.com/garipew/tilemgr/commit/a450c6593d0e32cbc9f8cba9c0a0dab69be38453))
- Add export route ([commit](https://github.com/garipew/tilemgr/commit/b8a9f40e72b541f265244fce125e308e29963b3e))
- Add RLE compression of canvas([commit](https://github.com/garipew/tilemgr/commit/2f878aa989d5851451186e0692cd1f9d76f92b88))
- Fix bug on selector, only works until specific height ([commit](https://github.com/garipew/tilemgr/commit/e984acbd3d63aef5a618b32ba26825f77e3981a5))
- Use canvas as viewport ([commit](https://github.com/garipew/tilemgr/commit/439e8497fe1c73130cd3b2fed0f1d69011a2dc60))
- Add zoom ([commit](https://github.com/garipew/tilemgr/commit/10c5996b82fa970b46db4b2e7b4263dae0ea91df))
- Update editor page layout ([commit](https://github.com/garipew/tilemgr/commit/42762836b87240f87d0da56f9f620b3e8bbfcad9))
- Modularize editor.js ([commit](https://github.com/garipew/tilemgr/commit/aaec265177cd5f4dbe04d652bfce45b422ef7fa8))
- Update README, add roadmap([commit](https://github.com/garipew/tilemgr/commit/a57ce6344bc38e938586274174cac7b384e8654e))
- Add EF Core ([commit](https://github.com/garipew/tilemgr/commit/4112eb6584e408a50b6049bdb7f4408d190e938c))
- Update DB to PostgreSQL ([commit](https://github.com/garipew/tilemgr/commit/4b2d43e831ae45f7d0ee53d47aa98512dee00bdb))
- Add Dockerfile and compose.yaml ([commit](https://github.com/garipew/tilemgr/commit/94a4708f15ea1cbb795ab2bf087a6a105676b608))

## Contributing

Issues and pull requests are welcomed.

## License

This project is licensed under MIT license.
