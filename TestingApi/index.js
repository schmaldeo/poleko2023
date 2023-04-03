const express = require("express");
const cors = require("cors");

const app = express();

app.use(cors());

app.get("/api/v1/school/status", (req, res) => {
  res.json({
    IS_RUNNING: true,
    TEMPERATURE_MAIN: {
      value: Math.round(Math.random() * 10000),
      error: false
    }
  })
});

const port = 56000;
app.listen(port, () => {
  console.log(`listening on ${port}`);
});
