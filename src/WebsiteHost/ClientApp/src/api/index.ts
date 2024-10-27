import axios, { AxiosError, HttpStatusCode } from "axios";
import { client as apiHost1 } from "./apiHost1/services.gen";
import { client as websiteHost1, refreshToken } from "./websiteHost/services.gen";

const ignoredFailingRequestUrls: string[] = ["/api/auth/refresh", "/api/auth"];

// This function sets up the appropriate request headers and handlers,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
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

  apiHost1.instance.interceptors.response.use((res) => res, handleFailedRequest);
  websiteHost1.instance.interceptors.response.use((res) => res, handleFailedRequest);
}

// This handles refreshing access tokens when any request returns a 401,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
async function handleFailedRequest(error: AxiosError) {
  const failingRequest = error.config;

  if (!failingRequest) {
    return Promise.reject(error);
  }

  // We don't want to retry one of the ignored API calls
  if (ignoredFailingRequestUrls.includes(failingRequest.url!)) {
    return Promise.reject(error);
  }

  if (error.status !== HttpStatusCode.Unauthorized) {
    return Promise.reject(error);
  }

  try {
    // Attempt to refresh the access_token cookies (if exist)
    await refreshToken();

    // Retry the original request
    return axios(failingRequest);
  } catch (error) {
    if (axios.isAxiosError(error)) {
      if (error.status === HttpStatusCode.Unauthorized || error.status === HttpStatusCode.Locked) {
        // Access token does not exist, or Refresh token is expired, or User is locked and cannot be refreshed,
        // or they cannot be authenticated anymore, the best we can do here is force the user to login again,
        // by re-fetching index.html, and refreshing CSRF token and cookie
        window.location.assign("/login");
        return Promise.reject(error);
      }
    }
  }

  return Promise.reject(error);
}

export { initializeApiClient };
