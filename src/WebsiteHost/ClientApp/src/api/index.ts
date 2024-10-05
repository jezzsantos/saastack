import { client as apiHost1 } from "./apiHost1/services.gen";
import { client as websiteHost1 } from "./websiteHost/services.gen";

function initializeApiClient() {
  const csrfToken = document.querySelector("meta[name='csrf-token']")?.getAttribute("content");
  apiHost1.setConfig({
    baseURL: `${process.env.WEBSITEHOSTBASEURL}/api`,
    headers: {
      accept: "application/json",
      "content-type": "application/json",
      "anti-csrf-tok": csrfToken
    }
  });
  websiteHost1.setConfig({
    baseURL: `${process.env.WEBSITEHOSTBASEURL}`,
    headers: {
      accept: "application/json",
      "content-type": "application/json",
      "anti-csrf-tok": csrfToken
    }
  });
}

export { initializeApiClient };
