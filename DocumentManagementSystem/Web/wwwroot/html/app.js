const apiUrl = 'https://localhost:8081/api/v1/documents';

// Function to fetch documents
function fetchDocuments() {
	console.log('Fetching documents...');
	document.getElementById('searchTerm').value = '';
	fetch(apiUrl)
		.then(response => response.json())
		.then(data => renderDocuments(data))
		.catch(error => console.error('Error fetching documents:', error));
}

// Function to search documents
function searchDocuments() {
	const searchTerm = document.getElementById('searchTerm').value.trim();
	const errorDiv = document.getElementById('errorMessages');

	if (!searchTerm) {
		fetchDocuments();
		return;
	}

	fetch(`${apiUrl}/fuzzy-search`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(searchTerm)
	})
		.then(response => {
			if (!response.ok) throw new Error('Failed to fetch search results.');
			return response.json();
		})
		.then(data => {
			errorDiv.innerHTML = '';
			renderDocuments(data);
		})
		.catch(error => {
			console.error('Error searching documents:', error);
			errorDiv.innerHTML = 'Error occurred while searching documents.';
		});
}

// Function to render documents
function renderDocuments(documents) {
	const documentList = document.getElementById('documentList');
	documentList.innerHTML = ''; // Clear the list before appending new items

	if (!documents || documents.length === 0) {
		documentList.innerHTML = '<li>No documents found.</li>';
		return;
	}

	documents.forEach(doc => {

		const li = document.createElement('li');
		li.style.display = 'flex';
		li.style.justifyContent = 'space-between';
		li.style.alignItems = 'center';

		const span = document.createElement('span');
		span.textContent = `Document ID: ${doc.id} | Document Name: ${doc.name}`;
		li.appendChild(span);

		const buttonContainer = document.createElement('div');

		// delete button
		const deleteButton = document.createElement('button');
		deleteButton.textContent = 'Delete';
		deleteButton.className = 'delete';
		deleteButton.style.marginLeft = '10px';
		deleteButton.onclick = () => deleteDocument(doc.id);
		buttonContainer.appendChild(deleteButton);

		// add "Content" button if OcrContent exists
		if (doc.ocrContent) {
			const contentButton = document.createElement('button');
			contentButton.textContent = 'Content';
			contentButton.style.marginLeft = '10px';
			contentButton.onclick = () => showOcrContentPopup(doc.ocrContent);
			buttonContainer.appendChild(contentButton);
		}

		li.appendChild(buttonContainer);

		documentList.appendChild(li);

	});
}

function showOcrContentPopup(content) {
	const popup = document.createElement('div');
	popup.className = 'popup';

	const popupContent = document.createElement('div');
	popupContent.className = 'popup-content';

	const closeButton = document.createElement('button');
	closeButton.className = 'popup-close';
	closeButton.textContent = 'X';
	closeButton.onclick = () => popup.remove();

	const contentText = document.createElement('p');
	contentText.textContent = content;

	popupContent.appendChild(closeButton);
	popupContent.appendChild(contentText);
	popup.appendChild(popupContent);
	document.body.appendChild(popup);
}



// Function to add a new document
function addDocument() {
	const errorDiv = document.getElementById('errorMessages');
	const fileInput = document.getElementById('documentFile');
	const file = fileInput.files[0];

	if (!file) {
		alert('Please select a file to upload');
		return;
	}

	const formData = new FormData();
	formData.append('file', file);

	fetch(apiUrl, {
		method: 'POST',
		body: formData
	})
		.then(response => {
			if (response.ok) {
				fetchDocuments();
				fileInput.value = '';
				errorDiv.innerHTML = '';
			} else {
				response.json().then(err => {
					errorDiv.innerHTML = `<ul>` + Object.values(err.errors).map(e => `<li>${e}</li>`).join('') + `</ul>`;
				});
			}
		})
		.catch(error => console.error('Error:', error));
}

// Function to delete a document
function deleteDocument(id) {
	fetch(`${apiUrl}/${id}`, {
		method: 'DELETE'
	})
		.then(response => {
			if (response.ok) {
				fetchDocuments();
			} else {
				console.error('Error deleting document.');
			}
		})
		.catch(error => console.error('Error:', error));
}

// Load document items on page load
document.addEventListener('DOMContentLoaded', fetchDocuments);
