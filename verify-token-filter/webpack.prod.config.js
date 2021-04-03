const path = require('path');
const nodeExternals = require('webpack-node-externals');
const Dotenv = require('dotenv-webpack');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');

module.exports = {
  mode: 'production',
  entry: './src/handler.ts',
  target: 'node',
  // externals: [nodeExternals()],
  output: {
    path: path.resolve(__dirname, 'build'),
    filename: 'handler.js',
    // libraryTarget: 'commonjs2',
  },
  plugins: [new Dotenv(), new CleanWebpackPlugin()],
  devtool: false,
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: [
          {
            loader: 'ts-loader',
          },
        ],
      },
    ],
  },
  resolve: {
    extensions: ['.ts', '.js'],
  },
};
