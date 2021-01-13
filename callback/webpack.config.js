const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const Dotenv = require('dotenv-webpack');

module.exports = {
  mode: 'development',
  entry: './src/index.ts',
  devtool: 'inline-source-map',
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      }
    ]
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
    modules: [
      "node_modules"
    ]
  },
  output: {
    filename: 'index.js',
    path: path.resolve(__dirname, 'build'),
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: './src/template.html',
      filename: 'index.html'
    }),
    new Dotenv(),
  ],
  devServer: {
    contentBase: path.join(__dirname, 'build'),
    // publicPath: path.resolve(__dirname, 'build'),
    // hot: true,
    index: 'index.html',
    open: true,
    //https://stackoverflow.com/questions/31602697/webpack-dev-server-cors-issue
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, PATCH, OPTIONS",
      "Access-Control-Allow-Headers": "*"
    },
    https: true
  }
};
