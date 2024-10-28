import { recorder } from "./recorder";
import { getOrganization, getProfileForCaller } from "./api/apiHost1";
import { initializeApiClient } from "./api";
import { showLoginForm } from "./pages/login";
import { getFeatureFlagForCaller, logout } from "./api/websiteHost";

initializeApiClient();

if (window.isTestingOnly) {
  recorder.traceInformation(`TESTINGONLY is enabled, and this app is HOSTED-ON: ${window.isHostedOn}`);
}

// TODO: add this to your routing logic, when the route changes
recorder.trackPageView(window.location.pathname);

// TODO: call this first to determine the current user
getProfileForCaller().then((res) => {
  //TODO: redirect the user to the login page
  if (!res.data?.profile?.isAuthenticated) {
    recorder.traceInformation(`User is signed out`);
  } else {
    recorder.traceInformation(
      `User is signed in as: ${res.data?.profile?.name?.firstName} ${res.data?.profile?.name?.lastName}`
    );
    const defaultOrganizationId = res.data?.profile?.defaultOrganizationId!;
    if (window.location.pathname === "/") {
      if (defaultOrganizationId && defaultOrganizationId.length > 0) {
        getOrganization({ path: { Id: defaultOrganizationId } }).then((res) =>
          recorder.traceInformation(
            `Their organization is (${res.data?.organization?.ownership}): ${res.data?.organization?.name}`
          )
        );
      }
      // @ts-ignore
      document.querySelector("#person").innerHTML =
        `${res.data?.profile?.name?.firstName} ${res.data?.profile?.name?.lastName}`;
    }
  }
});

// TODO: make these kinds of calls in your other code (don't use console.log directly!)
getFeatureFlagForCaller({ path: { Name: "a_feature_flag" } }).then((res) =>
  recorder.traceInformation(`a_feature_flag: ${res.data?.flag?.isEnabled}`)
);
//recorder.Crash(new Error("an actual error"), "record a fatal crash in JS app");
//recorder.TraceDebug("log a debug message");
//recorder.TraceInformation("log an message");

if (window.location.pathname === "/") {
  // @ts-ignore
  document.getElementById("root").innerHTML = `
    <h1>Welcome to SaaStack (<span style="font-size: small" id="person"></span>)</h1>
    <form id="logoutForm">
      <button type="button">Sign out</button>
    </form>
  `;

  document
    .querySelector("#logoutForm button")
    ?.addEventListener("click", () => logout().then(() => window.location.assign("/login")));
}

if (location.pathname === "/login") {
  showLoginForm();
}
