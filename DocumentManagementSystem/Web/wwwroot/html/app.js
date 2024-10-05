const apiUrl = 'https://localhost:8081/api/v1/documents';

// Function to fetch documents
function fetchDocuments() {
    console.log('Fetching documents...');
    fetch(apiUrl)
        .then(response => response.json())
        .then(data => {
            const documentList = document.getElementById('documentList');
            documentList.innerHTML = ''; // Clear the list before appending new items
            data.forEach(doc => {
                // Create list item with delete and toggle complete buttons
                const li = document.createElement('li');
                li.innerHTML = `
                    <span>Document id: ${doc.id} | Document name: ${doc.name}</span>
                    <button class="delete" style="margin-left: 10px;" onclick="deleteDocument(${doc.id})">Delete</button>
                    `;
                documentList.appendChild(li);
            });
        })
        .catch(error => console.error('Fehler beim Abrufen der Documents:', error));
}


// Function to add a new document
function addDocument() {
    const fileInput = document.getElementById('documentFile'); // Nehme den File-Input vom HTML
    const file = fileInput.files[0]; // Die ausgewählte Datei

    if (!file) {
        alert('Please select a file to upload');
        return;
    }

    // FormData verwenden, um die Datei zu übermitteln
    const formData = new FormData();
    formData.append('file', file);

    fetch(apiUrl, {
        method: 'POST',
        body: formData // Sende FormData mit der Datei
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Aktualisiere die Liste nach dem Upload
                fileInput.value = ''; // Leere das File-Input-Feld
            } else {
                response.json().then(err => alert("Fehler: " + err.message));
                console.error('Fehler beim Hochladen.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}


// Function to delete a docuemnt
function deleteDocument(id) {
    fetch(`${apiUrl}/${id}`, {
        method: 'DELETE'
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Refresh the list after deletion
            } else {
                console.error('Fehler beim Löschen.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}


// Load document items on page load
document.addEventListener('DOMContentLoaded', fetchDocuments);
