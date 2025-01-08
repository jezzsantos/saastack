const path = require("path");
const Dotenv = require("dotenv-webpack");
const AssetsPluginInstance = require("assets-webpack-plugin");

module.exports = (env, argv) => {
  const isProduction = argv.mode === "production";
  const envVars = isProduction ? "./.env.deploy" : "./.env";
  return {
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
        path: envVars,
        defaults: false,
        safe: false,
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
};
