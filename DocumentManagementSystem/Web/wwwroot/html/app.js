const apiUrl = 'https://localhost:8081/api/v1/documents';

// Function to fetch documents
function fetchDocuments() {
    console.log('Fetching documents...');
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
        errorDiv.innerHTML = 'Please enter a search term.';
        return;
    }

    fetch(`${apiUrl}/query-search`, {
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
        li.innerHTML = `
            <span>Document ID: ${doc.id} | Document Name: ${doc.name}</span>
            <button class="delete" style="margin-left: 10px;" onclick="deleteDocument(${doc.id})">Delete</button>
        `;
        documentList.appendChild(li);
    });
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
