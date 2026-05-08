let total = 0;

async function getProjectList() {
	const response = await fetch("/projects/list");

	if(!response.ok) {
		throw new Error(`HTTP error! status: ${response.status}`);
	}

	return await response.json();
}

async function requestDelete(path) {
	const response = await fetch(path, { method: 'DELETE', });

	if(response.ok) {
		const deletedProjectDiv = document.getElementById(path);
		deletedProjectDiv.remove();
		total--;
		displayEmpty();
	}
}

function displayEmpty() {
	if(total == 0) {
		const emptyDiv = document.createElement("div");
		emptyDiv.innerHTML = `<p>No project created yet.</p>

				<a href=\"/projects/new\" class=\"btn\">Click here to create one</a>`;
		container.appendChild(emptyDiv);
	}
}

getProjectList().then(list => {
	total = list.length;
	displayEmpty();
	for(const p of list) {
		const creationDate = new Date(p.CreationDate);

		const projectDiv = document.createElement("div");
		projectDiv.id = `${p.path}`
		projectDiv.classList.add("project");
		projectDiv.classList.add("card");

		const headerDiv = document.createElement("div");
		headerDiv.innerHTML = `	<div>
						<a href=\"${p.path}/\">
							<h3>${p.name}</h3>
						</a>
					</div>
					<div>
						<button onClick=\"requestDelete('${p.path}')\" class=\"btn\">Delete</button>
					</div>`;
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
