import { HttpStatusCode } from "axios";
import { authenticate } from "../api/websiteHost";

function login(form: HTMLFormElement) {
  authenticate({
    body: {
      password: form.password.value,
      provider: "credentials",
      username: form.username.value
    }
  }).then((res) => {
    if (res.status === HttpStatusCode.Ok) {
      window.location.assign("/");
    }
  });
}

export function showLoginForm() {
  // @ts-ignore
  document.getElementById("root").innerHTML = `
    <h1>Sign in</h1>
    <form id="loginForm">
      <label for="username">Username:</label>
      <input type="text" id="username" name="username" value="auser@company.com" autocomplete="username" required>
      <br>
      <label for="password">Password:</label>
      <input type="password" id="password" name="password" value="1Password!" autocomplete="current-password" required>
      <br>
      <button id="home" type="button">Go Home</button>&nbsp;&nbsp;<button id="login" type="button">Sign in</button>
    </form>
  `;

  document
    .querySelector("#loginForm #login")
    ?.addEventListener("click", () => login(document.querySelector("form") as HTMLFormElement));
  document.querySelector("#loginForm #home")?.addEventListener("click", () => window.location.assign("/"));
}
