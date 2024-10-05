import { recorder } from "./recorder";
import { getProfileForCaller } from "./api/apiHost1";
import { initializeApiClient } from "./api";

initializeApiClient();

if (window.isTestingOnly) {
  recorder.TraceInformation("TESTINGONLY is enabled, and this app is HOSTED-ON " + window.isHostedOn + "!");
}

recorder.Crash(new Error("actual error"), "an error message");
recorder.TraceDebug("an debug message");
recorder.TraceInformation("an info message");

// TODO: add this to your routing logic
recorder.TrackPageView(window.location.pathname);

getProfileForCaller().then((res) => {
  //TODO: redirect the user to the login page
  if (res.data?.profile?.isAuthenticated === false) {
    // window.location.href = "/login";
  }
});
