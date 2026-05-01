// Get project list
async function getProjectList() {
	const response = await fetch("/projects/list");

	if(!response.ok) {
		throw new Error(`HTTP error! status: ${response.status}`);
	}

	return await response.json();
}

getProjectList().then(list => {
	for(const p of list) {
		const creationDate = new Date(p.CreationDate);

		const projectDiv = document.createElement("div");
		projectDiv.classList.add("project");

		const headerDiv = document.createElement("div");
		headerDiv.innerHTML = `	<a href=\"${p.path}/\">
						<h3>${p.name}</h3>
					</a>`;
		headerDiv.classList.add("header");

		const detailsDiv = document.createElement("div");
		detailsDiv.innerHTML = `<div>Tile size: ${p.TileWid} × ${p.TileHei} px</div>
					<div>Project size: ${p.Wid} × ${p.Hei} tiles</div>
					<div>Created: ${creationDate.toDateString()}</div>`;
		detailsDiv.classList.add("details");

		projectDiv.appendChild(headerDiv);
		projectDiv.appendChild(detailsDiv);
		container.appendChild(projectDiv);
	}
});
