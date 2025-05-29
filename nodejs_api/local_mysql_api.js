const express = require('express');
const mysql = require('mysql2');
const cors = require('cors');
const app = express();
const port = 3000;

// Middleware to parse JSON requests and enable CORS
app.use(express.json());
app.use(cors());

// Create a MySQL connection
const db = mysql.createConnection({
host: 'localhost',
user: 'root',
password: 'root',
database: 'PublicSchema' 
});

// Connect to MySQL
db.connect((err) => {
if (err) {
console.error('Error connecting to MySQL:', err);
return;
}
console.log('Connected to MySQL database');
});

// API endpoint to execute SQL queries
app.post('/query', (req, res) => {
console.log('Received body:', req.body);
const sqlQuery = req.body.query;

if (!sqlQuery) {
return res.status(400).json({ error: 'SQL query is required' });
}

db.query(sqlQuery, (err, results) => {
if (err) {
console.error('Error executing query:', err);
return res.status(500).json({ error: 'Failed to execute query', details: err.message });
}

res.json(results);
});
});

// Start the server
app.listen(port, () => {
console.log('Server running on port ', port);
});