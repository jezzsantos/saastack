import axios, { AxiosError, HttpStatusCode } from "axios";
import { client as apiHost1 } from "./apiHost1/services.gen";
import { client as websiteHost, refreshToken } from "./websiteHost/services.gen";
import { recorder } from "../recorder";
import { ProblemDetails } from "./websiteHost";
import { UsageConstants } from "../UsageConstants";

const unRetryableRequestUrls: string[] = ["/api/auth/refresh", "/api/auth"];
const loginPath = "/login";

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
    },
    paramsSerializer: {
      indexes: null // To prevent axios from encoding array indexes in the query string
    }
  });
  websiteHost.setConfig({
    baseURL: `${process.env.WEBSITEHOSTBASEURL}`,
    headers: {
      accept: "application/json",
      "content-type": "application/json",
      "anti-csrf-tok": csrfToken
    },
    paramsSerializer: {
      indexes: null // To prevent axios from encoding array indexes in the query string
    }
  });

  apiHost1.instance.interceptors.response.use((res) => res, handleUnauthorizedResponse);
  websiteHost.instance.interceptors.response.use((res) => res, handleUnauthorizedResponse);
}

// This handles refreshing access tokens when any request returns a 401,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
async function handleUnauthorizedResponse(error: AxiosError) {
  const requestConfig = error.config;

  //Handle 403's for CSRF
  const problem = error as AxiosError<ProblemDetails>;
  if (
    error.status === HttpStatusCode.Forbidden &&
    problem != undefined &&
    problem.response?.data.title === "csrf_violation"
  ) {
    forceLogin();
    return Promise.reject(error);
  }

  // Only handle 401s
  if (error.status !== HttpStatusCode.Unauthorized) {
    return Promise.reject(error);
  }

  // Check it is an axios response (i.e. has config)
  if (!requestConfig) {
    return Promise.reject(error);
  }

  // We don't want to retry any of these API calls
  if (unRetryableRequestUrls.includes(requestConfig.url!)) {
    return Promise.reject(error);
  }

  try {
    // Attempt to refresh the access_token cookies (if exist)
    return await refreshToken().then(async (res) => {
      if (axios.isAxiosError(res)) {
        const error = res as AxiosError;
        if (error.status === HttpStatusCode.Unauthorized || error.status === HttpStatusCode.Locked) {
          // Access token does not exist, or Refresh token is expired, or User is locked and cannot be refreshed,
          // or they cannot be authenticated anymore, the best we can do here is force the user to login again
          forceLogin();
        }
        return Promise.reject(error);
      } else {
        recorder.trackUsage(UsageConstants.UsageScenarios.BrowserAuthRefresh);
        // Retry the original request
        return axios.request(requestConfig).then((res) => {
          if (axios.isAxiosError(res)) {
            const error = res as AxiosError;
            if (error.status === HttpStatusCode.Unauthorized) {
              // User is not authenticated anymore, the best we can do here is force the user to login again
              forceLogin();
            }
            return Promise.reject(error);
          } else {
            return res;
          }
        });
      }
    });
  } catch (error) {
    return Promise.reject(error);
  }
}

// Send the user to login, by re-fetching index.html, and refreshing CSRF token and cookie
function forceLogin() {
  window.location.assign(loginPath);
}

export { initializeApiClient };
