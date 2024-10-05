import * as fs from "fs";
import * as https from "https";
import { createClient } from "@hey-api/openapi-ts";
import "dotenv/config";

function updateApis(serverName, swaggerUrl, targetDefinitionFile, targetFolder) {
  let downloadFilename = targetDefinitionFile + ".tmp";
  let downloadedFile = fs.createWriteStream(downloadFilename);
  downloadedFile.on("close", () => {
    if (downloadedFile.bytesWritten > 0) {
      fs.copyFileSync(downloadFilename, targetDefinitionFile);
      formatLog("Replacing API definitions in ", targetDefinitionFile);
      generateApiServices(targetDefinitionFile, targetFolder);
    }
  });

  https
    .get(
      swaggerUrl,
      {
        headers: {
          Connection: "keep-alive"
        },
        rejectUnauthorized: false
      },
      (response) => {
        response.pipe(downloadedFile);
        response.on("end", () => downloadedFile.close());
      }
    )
    .on("error", (e) => {
      if (e.code === "ECONNREFUSED") {
        formatLog(
          `The "${serverName}" server (at ${swaggerUrl}) is not running, and we cannot download the swagger from it. Please start the server.`
        );
      } else {
        formatLog(
          `Failed to download API definitions from the "${serverName}" server (at ${swaggerUrl}), error was:`,
          "\n",
          e
        );
      }
    });
}

function formatLog() {
  console.log("");
  console.log("-------");
  console.log(...arguments);
  console.log("-------");
}

function generateApiServices(definitionsFile, targetFolder) {
  createClient({
    client: "@hey-api/client-axios",
    input: definitionsFile,
    output: {
      path: targetFolder,
      format: "prettier"
    }
  });
}

updateApis(
  "ApiHost1",
  `${process.env.APIHOST1BASEURL}/swagger/v1/swagger.json`,
  "./src/api/apiHost1.swagger.gen.json",
  "./src/api/apiHost1"
);
updateApis(
  "WebsiteHost",
  `${process.env.WEBSITEHOSTBASEURL}/swagger/v1/swagger.json`,
  "./src/api/websiteHost.swagger.gen.json",
  "./src/api/websiteHost"
);
