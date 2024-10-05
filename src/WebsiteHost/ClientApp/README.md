# WebsiteHost JS App

## Packaging & Bundling

We use [WebPack](https://webpack.js.org/) by default to provide bundled assets in a single JS file, which is output to
the `wwwroot` folder, where some other static assets live (e.g. SEO images, and SEO configuration).

### Browser Caching

To ensure that we are updating our customers' browser each and every time the JS code changes, we must bust the browser
cache for the generated `bundle.js` file.

This is done by pre-pending a digest to the generated `???.bundle.js` file, and updating that in the server-side
`Index.html` file.

To make that possible, we use a webpack plugin called `assets-webpack-plugin` which outputs the webpack generated asset
metadata into a `webpack.build.json` file, which lives in the code.
Then, at runtime, we load this JSON file dynamically, read out the digest, and update the `Index.html` file with the new
digest (in `Index.cshtml`).

## API Definitions

We use AXIOS to call the APIs in the BEFFE.

We generate these services automatically for you, by examining the BEFFE API and the BACKEND APIs, and then generate the
services for you.
You can update those at any time by running `npm run update:apis`

For this to work properly you must run both the BEFFE and the BACKEND APIs locally, so that the OpenAPI swagger endpoint
is reachable.
