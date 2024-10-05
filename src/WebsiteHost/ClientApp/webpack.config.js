const path = require("path");
const Dotenv = require("dotenv-webpack");
const AssetsPluginInstance = require("assets-webpack-plugin");

module.exports = {
  entry: "./src/index.ts",
  devtool: "inline-source-map",
  module: {
    rules: [
      {
        test: /\.ts?$/,
        use: "ts-loader",
        exclude: /node_modules/
      }
    ]
  },
  resolve: {
    extensions: [".ts", ".js"]
  },
  output: {
    filename: "[contenthash].bundle.js",
    path: path.resolve(__dirname, "..", "wwwroot"),
    clean: {
      keep(asset) {
        return !asset.includes("bundle.js"); // Leave all other existing files in wwwroot
      }
    }
  },
  plugins: [
    new Dotenv({
      // example file defines valid environment variables
      safe: "./.env.example",
      // defines the default env values that will be used if no .env file is found
      defaults: "./.env.defaults",
      allowEmptyValues: true
    }),
    new AssetsPluginInstance({
      // dumps the output of the webpack build into the filename
      filename: "webpack.build.json",
      removeFullPathAutoPrefix: true,
      prettyPrint: true
    })
  ]
};
