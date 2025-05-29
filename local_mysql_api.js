const express = require('express');
const mysql = require('mysql2');
const app = express();

const db = mysql.createConnection({
  host: 'localhost',
  user: 'root',
  password: 'root',
  database: 'PublicSchema'
});

app.use(express.json());

app.post('/query', (req, res) => {
  const sqlQuery = req.body.query;
  db.query(sqlQuery, (err, results) => {
    if (err) return res.status(500).send(err);
    res.json(results);
  });
});

app.listen(3000, () => console.log('Server running on port 3000'));