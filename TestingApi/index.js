const express = require("express");
const cors = require("cors");

const app = express();

app.use(cors());

let temp = 1000;
app.get("/api/v1/school/status", (req, res) => {
  temp += 10;
  if (temp == 3000) {
    temp = 1000;
  }
  console.log(temp);
  res.json({
    IS_RUNNING: true,
    TEMPERATURE_MAIN: {
      value: temp,
      error: false
    }
  })
});

const port = 56000;
app.listen(port, () => {
  console.log(`listening on ${port}`);
});
